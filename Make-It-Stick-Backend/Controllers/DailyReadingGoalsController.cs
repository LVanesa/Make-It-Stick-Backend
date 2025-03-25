using Make_It_Stick_Backend.Data;
using Make_It_Stick_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Make_It_Stick_Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DailyReadingGoalsController : BaseController
    {
        public DailyReadingGoalsController(AppDbContext context) : base(context) { }

        // GET: api/DailyReadingGoals
        [HttpGet]
        public async Task<IActionResult> GetDailyGoal()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            var goal = await _context.DailyReadingGoals
                                     .FirstOrDefaultAsync(g => g.UserId == user.UserId);

            if (goal == null)
                return NotFound("No daily reading goal found.");

            return Ok(new
            {
                goal.DailyReadingGoalId,
                goal.MinutesPerDay,
                goal.CreatedAt,
                goal.UpdatedAt
            });
        }

        // POST: api/DailyReadingGoals
        [HttpPost]
        public async Task<IActionResult> CreateDailyGoal([FromBody] int minutesPerDay)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            var existing = await _context.DailyReadingGoals
                                         .FirstOrDefaultAsync(g => g.UserId == user.UserId);

            if (existing != null)
                return BadRequest("A daily reading goal already exists.");

            var goal = new DailyReadingGoal
            {
                UserId = user.UserId,
                MinutesPerDay = minutesPerDay,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.DailyReadingGoals.Add(goal);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDailyGoal), new { }, goal);
        }

        // PUT: api/DailyReadingGoals
        [HttpPut]
        public async Task<IActionResult> UpdateDailyGoal([FromBody] int minutesPerDay)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            var goal = await _context.DailyReadingGoals
                                     .FirstOrDefaultAsync(g => g.UserId == user.UserId);

            if (goal == null)
                return NotFound("No daily reading goal found.");

            goal.MinutesPerDay = minutesPerDay;
            goal.UpdatedAt = DateTime.UtcNow;

            _context.DailyReadingGoals.Update(goal);
            await _context.SaveChangesAsync();

            return Ok(goal);
        }

        // DELETE: api/DailyReadingGoals
        [HttpDelete]
        public async Task<IActionResult> DeleteDailyGoal()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            var goal = await _context.DailyReadingGoals
                                     .FirstOrDefaultAsync(g => g.UserId == user.UserId);

            if (goal == null)
                return NotFound("No daily reading goal found.");

            _context.DailyReadingGoals.Remove(goal);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Daily reading goal deleted." });
        }

        // GET: api/DailyReadingGoals/summary
        [HttpGet("summary")]
        public async Task<IActionResult> GetDailyGoalSummary()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            var today = DateTime.UtcNow.Date;

            var goal = await _context.DailyReadingGoals
                                     .FirstOrDefaultAsync(g => g.UserId == user.UserId);

            var progressToday = await _context.DailyProgresses
                                              .FirstOrDefaultAsync(dp => dp.UserId == user.UserId && dp.Date == today);

            // Calculează streak-ul
            int streak = 0;
            var yesterday = today.AddDays(-1);

            while (true)
            {
                var entry = await _context.DailyProgresses
                                          .FirstOrDefaultAsync(dp => dp.UserId == user.UserId && dp.Date == yesterday);

                if (entry == null || !entry.GoalReached) break;

                streak++;
                yesterday = yesterday.AddDays(-1);
            }

            return Ok(new
            {
                minutesPerDay = goal?.MinutesPerDay ?? 0,
                minutesReadToday = progressToday?.TotalMinutes ?? 0,
                streak
            });
        }


    }
}
