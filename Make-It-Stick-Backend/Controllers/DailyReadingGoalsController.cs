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

            // Validare input
            if (minutesPerDay < 1 || minutesPerDay > 180)
                return BadRequest("Daily goal must be between 1 and 180 minutes.");

            var goal = await _context.DailyReadingGoals
                                     .FirstOrDefaultAsync(g => g.UserId == user.UserId);

            if (goal == null)
                return NotFound("No daily reading goal found.");

            // Salvează goal-ul vechi pentru logging
            int oldGoal = goal.MinutesPerDay;

            // Actualizează goal-ul
            goal.MinutesPerDay = minutesPerDay;
            goal.UpdatedAt = DateTime.UtcNow;

            _context.DailyReadingGoals.Update(goal);

            // RECALCULEAZĂ toate DailyProgress entries cu noul goal
            var allDailyProgresses = await _context.DailyProgresses
                .Where(dp => dp.UserId == user.UserId)
                .ToListAsync();

            int updatedRecords = 0;

            foreach (var progress in allDailyProgresses)
            {
                // Recalculează GoalReached cu noul goal
                bool newGoalReached = progress.TotalMinutes >= minutesPerDay;

                if (progress.GoalReached != newGoalReached)
                {
                    progress.GoalReached = newGoalReached;
                    progress.LastUpdated = DateTime.UtcNow;
                    _context.DailyProgresses.Update(progress);
                    updatedRecords++;
                }
            }

            await _context.SaveChangesAsync();

            // Log pentru debugging
            Console.WriteLine($"Goal updated from {oldGoal} to {minutesPerDay} minutes. Recalculated {updatedRecords} daily progress records.");

            return Ok(new
            {
                goal.DailyReadingGoalId,
                goal.MinutesPerDay,
                goal.UpdatedAt,
                recalculatedRecords = updatedRecords,
                message = $"Goal updated and {updatedRecords} progress records recalculated."
            });
        }

        // BONUS: Endpoint pentru recalculare manuală (useful pentru debugging)
        [HttpPost("recalculate")]
        public async Task<IActionResult> RecalculateAllProgress()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            var goal = await _context.DailyReadingGoals
                                     .FirstOrDefaultAsync(g => g.UserId == user.UserId);

            if (goal == null)
                return BadRequest("No daily reading goal found. Set a goal first.");

            // 🔥 Recalculează toate DailyProgress entries
            var allDailyProgresses = await _context.DailyProgresses
                .Where(dp => dp.UserId == user.UserId)
                .ToListAsync();

            int updatedRecords = 0;
            int totalRecords = allDailyProgresses.Count;

            foreach (var progress in allDailyProgresses)
            {
                bool newGoalReached = progress.TotalMinutes >= goal.MinutesPerDay;

                if (progress.GoalReached != newGoalReached)
                {
                    progress.GoalReached = newGoalReached;
                    progress.LastUpdated = DateTime.UtcNow;
                    _context.DailyProgresses.Update(progress);
                    updatedRecords++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                currentGoal = goal.MinutesPerDay,
                totalRecords,
                updatedRecords,
                message = $"Recalculated {updatedRecords} out of {totalRecords} progress records."
            });
        }

        // ENHANCED: GetDailyGoalSummary cu recalculare automată
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
            bool goalReachedToday = minutesReadToday >= minutesPerDay && minutesPerDay > 0;

            // 2. Salvează/actualizează în DailyProgress
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

            // 3. Calculează streak-ul corect cu goal check
            int streak = 0;
            if (goal != null && minutesPerDay > 0)
            {
                // Începe cu ziua de azi doar dacă goal-ul e atins
                if (goalReachedToday)
                {
                    streak = 1;
                    var currentDate = today.AddDays(-1);

                    // Verifică zilele anterioare
                    for (int i = 0; i < 100; i++) // limitează la 100 zile pentru performance
                    {
                        var dayProgress = await _context.DailyProgresses
                            .FirstOrDefaultAsync(dp => dp.UserId == user.UserId && dp.Date == currentDate);

                        if (dayProgress != null && dayProgress.TotalMinutes >= minutesPerDay)
                        {
                            streak++;
                            currentDate = currentDate.AddDays(-1);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                minutesPerDay,
                minutesReadToday,
                goalReached = goalReachedToday,
                streak,
                hasValidGoal = goal != null && minutesPerDay > 0
            });
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

    


    }
}
