using Make_It_Stick_Backend.Data;
using Make_It_Stick_Backend.Models;
using Make_It_Stick_Backend.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Make_It_Stick_Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TagsController : BaseController
    {

        public TagsController(AppDbContext context) : base(context) { }

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


    }
}
