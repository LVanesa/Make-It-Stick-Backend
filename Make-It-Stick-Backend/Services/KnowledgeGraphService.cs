using Make_It_Stick_Backend.Models.Dto;
using Make_It_Stick_Backend.Models;
using Make_It_Stick_Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Make_It_Stick_Backend.Services
{
    public interface IKnowledgeGraphService
    {
        Task<KnowledgeGraphDto> GetKnowledgeGraphAsync(int userId);
    }

    // DTO pentru statistici interne
    public class TagStatDto
    {
        public Tag Tag { get; set; } = default!;
        public int NotesCount { get; set; }
        public DateTime? LastUsed { get; set; }
    }

    public class KnowledgeGraphService : IKnowledgeGraphService
    {
        private readonly AppDbContext _context;
        private readonly Random _random = new Random();

        // Culori predefinite pentru tag-uri
        private readonly string[] _colors = {
            "#4A90E2", "#7ED321", "#F5A623", "#BD10E0", "#B8E986",
            "#50E3C2", "#FF6B6B", "#4ECDC4", "#45B7D1", "#96CEB4",
            "#FFB6C1", "#DDA0DD", "#98FB98", "#F0E68C", "#87CEEB"
        };

        public KnowledgeGraphService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<KnowledgeGraphDto> GetKnowledgeGraphAsync(int userId)
        {
            // 1. Obține toate tag-urile utilizatorului cu numărul de notițe
            var tagStatsRaw = await _context.Tags
                .Where(t => t.UserId == userId)
                .Select(t => new
                {
                    Tag = t,
                    NotesCount = t.BaseNoteTags.Count,
                    LastUsed = t.BaseNoteTags
                        .OrderByDescending(bnt => bnt.Note.CreatedAt)
                        .Select(bnt => bnt.Note.CreatedAt)
                        .FirstOrDefault()
                })
                .Where(ts => ts.NotesCount > 0) // Doar tag-urile care au notițe
                .ToListAsync();

            // Convertește la TagStatDto
            var tagStats = tagStatsRaw.Select(ts => new TagStatDto
            {
                Tag = ts.Tag,
                NotesCount = ts.NotesCount,
                LastUsed = ts.LastUsed
            }).ToList();

            if (!tagStats.Any())
            {
                return new KnowledgeGraphDto();
            }

            // 2. Calculează conexiunile între tag-uri
            var connections = await CalculateTagConnectionsAsync(userId, tagStats.Select(ts => ts.Tag.TagId).ToList());

            // 3. Calculează pozițiile pentru vizualizare (force-directed layout simplu)
            var tagNodes = GenerateTagNodes(tagStats, connections);

            // 4. Generează insights
            var insights = GenerateInsights(tagStats, connections);

            // 5. Calculează statistici
            var stats = CalculateStats(tagStats, connections);

            return new KnowledgeGraphDto
            {
                Tags = tagNodes,
                Connections = connections,
                Insights = insights,
                Stats = stats
            };
        }

        private async Task<List<TagConnectionDto>> CalculateTagConnectionsAsync(int userId, List<int> tagIds)
        {
            var connections = new List<TagConnectionDto>();

            // Query pentru a găsi notițele care au multiple tag-uri
            var notesWithMultipleTags = await _context.BaseNoteTags
                .Where(bnt => bnt.Note.UserId == userId && tagIds.Contains(bnt.TagId))
                .Include(bnt => bnt.Tag)
                .GroupBy(bnt => bnt.NoteId)
                .Where(group => group.Count() > 1) // Doar notițele cu mai mult de un tag
                .Select(group => group.Select(bnt => bnt.Tag.Name).ToList())
                .ToListAsync();

            // Calculează co-occurrence pentru fiecare pereche de tag-uri
            var tagPairCounts = new Dictionary<(string, string), int>();

            foreach (var noteTags in notesWithMultipleTags)
            {
                for (int i = 0; i < noteTags.Count; i++)
                {
                    for (int j = i + 1; j < noteTags.Count; j++)
                    {
                        var tag1 = noteTags[i];
                        var tag2 = noteTags[j];

                        // Asigură-te că ordinea este consistentă
                        var pair = string.Compare(tag1, tag2) < 0 ? (tag1, tag2) : (tag2, tag1);

                        if (tagPairCounts.ContainsKey(pair))
                        {
                            tagPairCounts[pair]++;
                        }
                        else
                        {
                            tagPairCounts[pair] = 1;
                        }
                    }
                }
            }

            // Convertește în ConnectionDto-uri
            foreach (var kvp in tagPairCounts)
            {
                if (kvp.Value >= 1) // Pragul minim pentru conexiune
                {
                    connections.Add(new TagConnectionDto
                    {
                        From = kvp.Key.Item1,
                        To = kvp.Key.Item2,
                        Strength = kvp.Value,
                        Weight = Math.Min(kvp.Value / 10.0, 1.0) // Normalizează la 0-1
                    });
                }
            }

            return connections;
        }

        private List<TagNodeDto> GenerateTagNodes(
            List<TagStatDto> tagStats,
            List<TagConnectionDto> connections)
        {
            var nodes = new List<TagNodeDto>();
            var maxNotes = tagStats.Max(ts => ts.NotesCount);

            // Calculează numărul de conexiuni pentru fiecare tag
            var connectionCounts = new Dictionary<string, int>();
            foreach (var connection in connections)
            {
                if (connectionCounts.ContainsKey(connection.From))
                {
                    connectionCounts[connection.From]++;
                }
                else
                {
                    connectionCounts[connection.From] = 1;
                }

                if (connectionCounts.ContainsKey(connection.To))
                {
                    connectionCounts[connection.To]++;
                }
                else
                {
                    connectionCounts[connection.To] = 1;
                }
            }

            for (int i = 0; i < tagStats.Count; i++)
            {
                var tagStat = tagStats[i];
                var tagName = tagStat.Tag.Name;

                nodes.Add(new TagNodeDto
                {
                    Id = tagName,
                    Name = tagName,
                    Size = Math.Max(15, (int)(25 * (double)tagStat.NotesCount / maxNotes) + 15), // 15-40 px
                    Connections = connectionCounts.ContainsKey(tagName) ? connectionCounts[tagName] : 0,
                    X = GenerateXPosition(i, tagStats.Count),
                    Y = GenerateYPosition(i, tagStats.Count),
                    Color = _colors[i % _colors.Length],
                    LastUsed = tagStat.LastUsed,
                    NotesCount = tagStat.NotesCount
                });
            }

            return nodes;
        }

        private double GenerateXPosition(int index, int total)
        {
            // Distribuie uniform pe orizontală cu un pic de randomness
            var baseX = 50 + (250.0 * index / Math.Max(1, total - 1));
            return baseX + _random.Next(-20, 21); // ±20px variație
        }

        private double GenerateYPosition(int index, int total)
        {
            // Distribuie cu pattern circular/spiral
            var angle = 2 * Math.PI * index / total;
            var radius = 60 + (index % 3) * 20; // Raze diferite pentru layere
            return 125 + radius * Math.Sin(angle); // Centru la Y=125
        }

        private KnowledgeGraphInsightsDto GenerateInsights(
            List<TagStatDto> tagStats,
            List<TagConnectionDto> connections)
        {
            var insights = new KnowledgeGraphInsightsDto();

            if (tagStats.Any())
            {
                // Core Concept - tag-ul cu cele mai multe notițe
                var coreTag = tagStats.OrderByDescending(ts => ts.NotesCount).First();
                insights.CoreConcept = new TagInsightDto
                {
                    Type = "core",
                    Icon = "target",
                    Title = "Your Core Concept",
                    Content = $"{coreTag.Tag.Name} is the center of your intellectual universe",
                    Detail = $"{coreTag.NotesCount} notes, {connections.Count(c => c.From == coreTag.Tag.Name || c.To == coreTag.Tag.Name)} strong connections",
                    Color = "#EBF4FF",
                    BorderColor = "#93C5FD",
                    TagName = coreTag.Tag.Name,
                    Value = coreTag.NotesCount
                };

                // Bridge Builder - tag-ul cu cele mai multe conexiuni
                var connectionCounts = new Dictionary<string, int>();
                foreach (var connection in connections)
                {
                    if (connectionCounts.ContainsKey(connection.From))
                    {
                        connectionCounts[connection.From]++;
                    }
                    else
                    {
                        connectionCounts[connection.From] = 1;
                    }

                    if (connectionCounts.ContainsKey(connection.To))
                    {
                        connectionCounts[connection.To]++;
                    }
                    else
                    {
                        connectionCounts[connection.To] = 1;
                    }
                }

                if (connectionCounts.Any())
                {
                    var bridgeTag = connectionCounts.OrderByDescending(kvp => kvp.Value).First();
                    insights.BridgeBuilder = new TagInsightDto
                    {
                        Type = "bridge",
                        Icon = "link",
                        Title = "Bridge Builder",
                        Content = $"{bridgeTag.Key} connects multiple knowledge domains",
                        Detail = $"See connections between {bridgeTag.Value} other concepts",
                        Color = "#FFF7ED",
                        BorderColor = "#FDBA74",
                        TagName = bridgeTag.Key,
                        Value = bridgeTag.Value
                    };
                }

                // Growing Interest - tag recent folosit
                var recentTag = tagStats
                    .Where(ts => ts.LastUsed.HasValue && ts.LastUsed > DateTime.UtcNow.AddDays(-30))
                    .OrderByDescending(ts => ts.LastUsed)
                    .FirstOrDefault();

                if (recentTag != null)
                {
                    insights.GrowingInterest = new TagInsightDto
                    {
                        Type = "growing",
                        Icon = "trending-up",
                        Title = "Growing Interest",
                        Content = $"{recentTag.Tag.Name} - intense recent activity",
                        Detail = "A new passion taking shape",
                        Color = "#F0FDF4",
                        BorderColor = "#86EFAC",
                        TagName = recentTag.Tag.Name,
                        Value = recentTag.NotesCount
                    };
                }

                // Unexplored Territory - tag-uri cu puține conexiuni
                var isolatedTag = tagStats
                    .Where(ts => !connectionCounts.ContainsKey(ts.Tag.Name) || connectionCounts[ts.Tag.Name] <= 1)
                    .OrderByDescending(ts => ts.NotesCount)
                    .FirstOrDefault();

                if (isolatedTag != null)
                {
                    insights.UnexploredTerritory = new TagInsightDto
                    {
                        Type = "opportunity",
                        Icon = "bulb",
                        Title = "Unexplored Territory",
                        Content = $"{isolatedTag.Tag.Name} stands isolated - ready to connect it?",
                        Detail = "Potential for connecting with other concepts",
                        Color = "#FAF5FF",
                        BorderColor = "#C084FC",
                        TagName = isolatedTag.Tag.Name,
                        Value = isolatedTag.NotesCount
                    };
                }
            }

            return insights;
        }

        private KnowledgeGraphStatsDto CalculateStats(
            List<TagStatDto> tagStats,
            List<TagConnectionDto> connections)
        {
            var totalTags = tagStats.Count;
            var totalNotes = tagStats.Sum(ts => ts.NotesCount);
            var maxPossibleConnections = totalTags * (totalTags - 1) / 2;
            var graphDensity = maxPossibleConnections > 0 ? (double)connections.Count / maxPossibleConnections * 100 : 0;

            var connectionCounts = new Dictionary<string, int>();
            foreach (var connection in connections)
            {
                if (connectionCounts.ContainsKey(connection.From))
                {
                    connectionCounts[connection.From]++;
                }
                else
                {
                    connectionCounts[connection.From] = 1;
                }

                if (connectionCounts.ContainsKey(connection.To))
                {
                    connectionCounts[connection.To]++;
                }
                else
                {
                    connectionCounts[connection.To] = 1;
                }
            }

            var isolatedTags = tagStats.Count(ts => !connectionCounts.ContainsKey(ts.Tag.Name));

            return new KnowledgeGraphStatsDto
            {
                TotalTags = totalTags,
                ActiveLinks = connections.Count,
                GraphDensity = Math.Round(graphDensity, 1),
                IsolatedTags = isolatedTags,
                TotalNotes = totalNotes,
                AverageTagsPerNote = totalNotes > 0 ? Math.Round((double)totalNotes / tagStats.Count, 1) : 0
            };
        }
    }
}