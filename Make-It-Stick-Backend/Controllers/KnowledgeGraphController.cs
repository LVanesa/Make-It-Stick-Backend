using Make_It_Stick_Backend.Models.Dto;
using Make_It_Stick_Backend.Services;
using Make_It_Stick_Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Make_It_Stick_Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class KnowledgeGraphController : BaseController
    {
        private readonly IKnowledgeGraphService _knowledgeGraphService;

        public KnowledgeGraphController(
            AppDbContext context,
            IKnowledgeGraphService knowledgeGraphService) : base(context)
        {
            _knowledgeGraphService = knowledgeGraphService;
        }

        [HttpGet]
        public async Task<IActionResult> GetKnowledgeGraph()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            int userId = user.UserId;

            try
            {
                var knowledgeGraph = await _knowledgeGraphService.GetKnowledgeGraphAsync(userId);

                if (knowledgeGraph.Tags.Count == 0)
                {
                    return Ok(new KnowledgeGraphDto
                    {
                        Tags = new List<TagNodeDto>(),
                        Connections = new List<TagConnectionDto>(),
                        Insights = new KnowledgeGraphInsightsDto(),
                        Stats = new KnowledgeGraphStatsDto()
                    });
                }

                return Ok(knowledgeGraph);
            }
            catch (Exception ex)
            {
                // Log error if you have logging configured
                return StatusCode(500, "An error occurred while generating the knowledge graph");
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetKnowledgeGraphStats()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            int userId = user.UserId;

            try
            {
                var knowledgeGraph = await _knowledgeGraphService.GetKnowledgeGraphAsync(userId);
                return Ok(knowledgeGraph.Stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while getting knowledge graph statistics");
            }
        }

        [HttpGet("insights")]
        public async Task<IActionResult> GetKnowledgeGraphInsights()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            int userId = user.UserId;

            try
            {
                var knowledgeGraph = await _knowledgeGraphService.GetKnowledgeGraphAsync(userId);
                return Ok(knowledgeGraph.Insights);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while getting knowledge graph insights");
            }
        }
    }
}