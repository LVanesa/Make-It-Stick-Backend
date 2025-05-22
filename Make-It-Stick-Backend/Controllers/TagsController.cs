using Make_It_Stick_Backend.Data;
using Make_It_Stick_Backend.Models;
using Make_It_Stick_Backend.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Make_It_Stick_Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TagsController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TagsController(AppDbContext context, IHttpClientFactory httpClientFactory) : base(context) {
            _httpClientFactory = httpClientFactory;
        }

        // POST: api/tags
        [HttpPost]
        public async Task<IActionResult> CreateTag([FromBody] TagDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            // Opțional: poți verifica dacă există deja un tag cu același nume pentru user
            var exists = await _context.Tags
                .AnyAsync(t => t.Name == dto.Name && t.UserId == user.UserId);

            if (exists)
                return BadRequest("Tag with the same name already exists.");

            var tag = new Tag
            {
                Name = dto.Name,
                UserId = user.UserId
            };

            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();

            return Ok(new { tagId = tag.TagId, name = tag.Name });
        }

        // GET: api/tags
        [HttpGet]
        public async Task<IActionResult> GetUserTags()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            var tags = await _context.Tags
                .Where(t => t.UserId == user.UserId)
                .Select(t => new
                {
                    t.TagId,
                    t.Name
                })
                .ToListAsync();

            return Ok(tags);
        }

        // POST: api/Tags/suggest
        [HttpPost("suggest")]
        public async Task<IActionResult> Suggest([FromBody] PredictRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Text))
                return BadRequest("Text must not be empty.");

            // 1) Prepară HTTP client către FastAPI
            var client = _httpClientFactory.CreateClient("FastApi");

            // 2) Trimite cererea la FastAPI
            HttpResponseMessage response;
            try
            {
                response = await client.PostAsJsonAsync("predict", new { text = dto.Text });
            }
            catch (Exception ex)
            {
                return StatusCode(502, $"Error calling ML service: {ex.Message}");
            }

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, $"ML service error: {err}");
            }

            // 3) Citește răspunsul { tags: [ "tag1", "tag2", … ] }
            using var stream = await response.Content.ReadAsStreamAsync();
            var doc = await JsonDocument.ParseAsync(stream);
            if (!doc.RootElement.TryGetProperty("tags", out var tagsElem) || tagsElem.ValueKind != JsonValueKind.Array)
                return StatusCode(502, "Invalid response from ML service.");

            // 4) Map la SuggestionDto
            var suggestions = tagsElem.EnumerateArray()
                .Select((je, idx) => new SuggestionDto
                {
                    TagId = $"sug-{idx}",
                    Name = je.GetString()!
                })
                .ToList();

            return Ok(suggestions);
        }

        // DELETE: api/Tags/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteTag(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized();

            // 1) Încarcă tag-ul cu doar junction-urile (fără să includă Note propriu-zisă)
            var tag = await _context.Tags
                .Include(t => t.BaseNoteTags)
                .FirstOrDefaultAsync(t => t.TagId == id && t.UserId == user.UserId);

            if (tag == null)
                return NotFound();

            // 2) Şterge doar legăturile tag–note
            _context.BaseNoteTags.RemoveRange(tag.BaseNoteTags);

            // 3) Şterge entitatea Tag
            _context.Tags.Remove(tag);

            // 4) Commit
            await _context.SaveChangesAsync();

            return NoContent();
        }

    }


}
