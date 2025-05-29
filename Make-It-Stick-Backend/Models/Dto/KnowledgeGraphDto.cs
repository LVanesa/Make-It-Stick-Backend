namespace Make_It_Stick_Backend.Models.Dto
{
    public class KnowledgeGraphDto
    {
        public List<TagNodeDto> Tags { get; set; } = new List<TagNodeDto>();
        public List<TagConnectionDto> Connections { get; set; } = new List<TagConnectionDto>();
        public KnowledgeGraphInsightsDto Insights { get; set; } = new KnowledgeGraphInsightsDto();
        public KnowledgeGraphStatsDto Stats { get; set; } = new KnowledgeGraphStatsDto();
    }

    public class TagNodeDto
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public int Size { get; set; } // Numărul de notițe
        public int Connections { get; set; } // Numărul de conexiuni
        public double X { get; set; } // Poziție X calculată
        public double Y { get; set; } // Poziție Y calculată
        public string Color { get; set; } = default!;
        public DateTime? LastUsed { get; set; }
        public int NotesCount { get; set; }
    }

    public class TagConnectionDto
    {
        public string From { get; set; } = default!;
        public string To { get; set; } = default!;
        public int Strength { get; set; } // Numărul de notițe comune
        public double Weight { get; set; } // Greutatea normalizată (0-1)
    }

    public class KnowledgeGraphInsightsDto
    {
        public TagInsightDto? CoreConcept { get; set; }
        public TagInsightDto? BridgeBuilder { get; set; }
        public TagInsightDto? GrowingInterest { get; set; }
        public TagInsightDto? UnexploredTerritory { get; set; }
    }

    public class TagInsightDto
    {
        public string Type { get; set; } = default!;
        public string Icon { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Content { get; set; } = default!;
        public string Detail { get; set; } = default!;
        public string Color { get; set; } = default!;
        public string BorderColor { get; set; } = default!;
        public string TagName { get; set; } = default!;
        public int Value { get; set; }
    }

    public class KnowledgeGraphStatsDto
    {
        public int TotalTags { get; set; }
        public int ActiveLinks { get; set; }
        public double GraphDensity { get; set; }
        public int IsolatedTags { get; set; }
        public int TotalNotes { get; set; }
        public double AverageTagsPerNote { get; set; }
    }
}
