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

            int minutesPerDay = goal?.MinutesPerDay ?? 0;

            // 1. Calculează totalul din sesiuni de lectură
            var todaySessions = await _context.ReadingSessions
                .Include(rs => rs.LibraryItem)
                .Where(rs => rs.LibraryItem.UserId == user.UserId && rs.StartTime.Date == today)
                .ToListAsync();

            int minutesReadToday = todaySessions.Sum(s => s.Duration ?? 0);
            bool goalReachedToday = minutesReadToday >= minutesPerDay;

            // 2. Salvează în DailyProgress
            var progress = await _context.DailyProgresses
                .FirstOrDefaultAsync(dp => dp.UserId == user.UserId && dp.Date == today);

            if (progress == null)
            {
                progress = new DailyProgress
                {
                    UserId = user.UserId,
                    Date = today,
                    TotalMinutes = minutesReadToday,
                    GoalReached = goalReachedToday,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };
                _context.DailyProgresses.Add(progress);
            }
            else
            {
                progress.TotalMinutes = minutesReadToday;
                progress.GoalReached = goalReachedToday;
                progress.LastUpdated = DateTime.UtcNow;
                _context.DailyProgresses.Update(progress);
            }

            // 3. Calculează streak-ul pornind de azi înapoi
            int streak = goalReachedToday ? 1 : 0;
            var currentDate = today.AddDays(-1);

            while (true)
            {
                var sessions = await _context.ReadingSessions
                    .Include(rs => rs.LibraryItem)
                    .Where(rs => rs.LibraryItem.UserId == user.UserId && rs.StartTime.Date == currentDate)
                    .ToListAsync();

                int total = sessions.Sum(s => s.Duration ?? 0);
                if (goal == null || total < minutesPerDay) break;

                streak++;
                currentDate = currentDate.AddDays(-1);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                minutesPerDay,
                minutesReadToday,
                goalReached = goalReachedToday,
                streak
            });
        }




    }
}
