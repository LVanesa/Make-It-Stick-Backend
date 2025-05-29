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
    public class InsightsController : BaseController
    {
        public InsightsController(AppDbContext context) : base(context) { }

        [HttpGet("reading-activity")]
        [Authorize]
        public async Task<IActionResult> GetReadingActivityHeatmap()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            int userId = user.UserId;

            // Setăm perioada de analiză – ultimele 6 luni
            var startDate = DateTime.UtcNow.Date.AddMonths(-6);
            var endDate = DateTime.UtcNow.Date;

            // Obținem toate sesiunile de lectură ale utilizatorului, inclusiv LibraryItem
            var sessions = await _context.ReadingSessions
                .Include(rs => rs.LibraryItem)
                .Where(rs => rs.LibraryItem.UserId == userId && rs.StartTime.Date >= startDate && rs.StartTime.Date <= endDate)
                .ToListAsync();

            // Grupăm sesiunile pe zi și calculăm totalul minutelor
            var grouped = sessions
                .Where(s => s.Duration.HasValue)
                .GroupBy(s => s.StartTime.Date)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(s => s.Duration ?? 0)
                );

            // Construim o listă completă de date (pentru zile fără activitate)
            var dateRange = Enumerable.Range(0, (endDate - startDate).Days + 1)
                .Select(offset => startDate.AddDays(offset))
                .ToList();

            // Formatăm rezultatul
            var result = dateRange.Select(date => new
            {
                date = date.ToString("yyyy-MM-dd"),
                minutesRead = grouped.ContainsKey(date) ? grouped[date] : 0
            });

            return Ok(result);
        }

        [HttpGet("pace-tracker")]
        [Authorize]
        public async Task<IActionResult> GetPaceTracker()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            var currentYear = DateTime.UtcNow.Year;

            var goal = await _context.YearlyReadingGoals
                .Where(g => g.UserId == user.UserId && g.Year == currentYear)
                .FirstOrDefaultAsync();

            if (goal == null)
            {
                return NotFound("No yearly goal found for current year.");
            }

            var today = DateTime.UtcNow;
            var daysInYear = DateTime.IsLeapYear(today.Year) ? 366 : 365;
            var dayOfYear = today.DayOfYear;
            var progressPercent = (double)dayOfYear / daysInYear;

            var expectedBooksByNow = Math.Round(goal.TargetBooks * progressPercent, 2);

            var booksFinished = await _context.LibraryItems
                .Where(b => b.UserId == user.UserId
                    && b.LibraryShelf == LibraryShelf.Finished
                    && b.DateFinished.HasValue
                    && b.DateFinished.Value.Year == currentYear)
                .CountAsync();

            var difference = Math.Round(booksFinished - expectedBooksByNow, 2);
            var paceStatus = difference switch
            {
                > 0 => "ahead",
                0 => "on-track",
                _ => "behind"
            };

            return Ok(new
            {
                yearGoal = goal.TargetBooks,
                booksFinished = booksFinished,
                expectedBooksByNow = expectedBooksByNow,
                paceStatus = paceStatus,
                difference = difference
            });
        }


        [HttpGet("daily-reading-progress")]
        public async Task<IActionResult> GetDailyReadingProgress()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            var today = DateTime.UtcNow.Date;

            // Start of week = Monday
            var diff = ((int)today.DayOfWeek + 6) % 7;
            var startOfWeek = today.AddDays(-diff);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            var goalEntity = await _context.DailyReadingGoals.FirstOrDefaultAsync(g => g.UserId == user.UserId);

            int goal = goalEntity?.MinutesPerDay ?? 0;

            // Fetch progresses from this month
            var dailyProgresses = await _context.DailyProgresses
                .Where(dp => dp.UserId == user.UserId && dp.Date >= startOfMonth)
                .ToListAsync();

            // Fetch reading sessions for productivity analysis
            var readingSessions = await _context.ReadingSessions
                .Include(rs => rs.LibraryItem)
                .Where(rs => rs.LibraryItem.UserId == user.UserId &&
                            rs.StartTime.Date >= startOfMonth &&
                            rs.Duration.HasValue)
                .ToListAsync();

            var todayProgress = dailyProgresses
                .FirstOrDefault(dp => dp.Date.Date == today)?.TotalMinutes ?? 0;

            // Weekly Data (Mon–Sun)
            var weeklyData = Enumerable.Range(0, 7).Select(offset =>
            {
                var date = startOfWeek.AddDays(offset);
                var dp = dailyProgresses.FirstOrDefault(d => d.Date.Date == date);
                int actual = dp?.TotalMinutes ?? 0;
                return new
                {
                    day = date.ToString("ddd"),
                    goal,
                    actual,
                    completed = actual >= goal
                };
            }).ToList();

            // Monthly stats
            int daysCompleted = dailyProgresses.Count(dp => dp.TotalMinutes >= goal);
            int totalDays = (today - startOfMonth).Days + 1;
            int activeDays = dailyProgresses.Count(dp => dp.TotalMinutes > 0);
            int totalMinutesRead = dailyProgresses.Sum(dp => dp.TotalMinutes);
            int avgDailyAllDays = totalDays > 0 ? totalMinutesRead / totalDays : 0;
            int avgDailyActiveDays = activeDays > 0 ? totalMinutesRead / activeDays : 0;

            // Calculate most productive time based on reading sessions
            var morningMinutes = readingSessions
                .Where(rs => rs.StartTime.Hour >= 6 && rs.StartTime.Hour < 12)
                .Sum(rs => rs.Duration ?? 0);

            var afternoonMinutes = readingSessions
                .Where(rs => rs.StartTime.Hour >= 12 && rs.StartTime.Hour < 18)
                .Sum(rs => rs.Duration ?? 0);

            var eveningMinutes = readingSessions
                .Where(rs => rs.StartTime.Hour >= 18 || rs.StartTime.Hour < 6)
                .Sum(rs => rs.Duration ?? 0);

            string mostProductiveTime = "morning"; // default
            var maxMinutes = Math.Max(morningMinutes, Math.Max(afternoonMinutes, eveningMinutes));

            if (maxMinutes > 0)
            {
                if (afternoonMinutes == maxMinutes)
                    mostProductiveTime = "afternoon";
                else if (eveningMinutes == maxMinutes)
                    mostProductiveTime = "evening";
                // else stays "morning"
            }

            // Current streak
            int streak = 0;
            for (int i = 0; i < 100; i++)
            {
                var date = today.AddDays(-i);
                var dp = dailyProgresses.FirstOrDefault(d => d.Date.Date == date);
                if (dp != null && dp.TotalMinutes >= goal)
                    streak++;
                else
                    break;
            }

            // Best streak (all time)
            var allProgresses = await _context.DailyProgresses
                .Where(dp => dp.UserId == user.UserId)
                .OrderBy(dp => dp.Date)
                .ToListAsync();

            int bestStreak = 0, currentStreak = 0;
            DateTime? lastDate = null;

            foreach (var dp in allProgresses)
            {
                if (dp.TotalMinutes >= goal)
                {
                    if (lastDate == null || (dp.Date - lastDate.Value).Days == 1)
                        currentStreak++;
                    else
                        currentStreak = 1;

                    bestStreak = Math.Max(bestStreak, currentStreak);
                    lastDate = dp.Date;
                }
                else
                {
                    currentStreak = 0;
                    lastDate = null;
                }
            }

            // Calculate reading pace (pages per minute) for additional insights
            var sessionsWithPages = readingSessions
                .Where(rs => rs.PagesRead.HasValue && rs.Duration.HasValue &&
                            rs.PagesRead > 0 && rs.Duration > 0)
                .ToList();

            double averageReadingPace = 0;
            if (sessionsWithPages.Any())
            {
                averageReadingPace = sessionsWithPages
                    .Average(rs => (double)rs.PagesRead.Value / rs.Duration.Value);
            }

            var result = new
            {
                todayGoal = goal,
                todayProgress,
                streak,
                weeklyData,
                mostProductiveTime,
                productivityBreakdown = new
                {
                    morning = morningMinutes,
                    afternoon = afternoonMinutes,
                    evening = eveningMinutes
                },
                monthlyStats = new
                {
                    daysCompleted,
                    totalDays,
                    averageDaily = avgDailyActiveDays,
                    averageDailyAllDays = avgDailyAllDays,
                    bestStreak,
                    currentMonth = today.ToString("MMMM"),
                    totalSessions = readingSessions.Count,
                    averageReadingPace = Math.Round(averageReadingPace, 2)
                }
            };

            return Ok(result);
        }

        [HttpGet("retention-forecast")]
        public async Task<IActionResult> GetRetentionForecast()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            int userId = user.UserId;

            // Preluăm toate întrebările (quiz) cu stability > 0
            var quizzes = await _context.QuizQuestions
                .Include(q => q.Note)
                .Where(q => q.Note.UserId == userId && q.Stability > 0)
                .ToListAsync();

            if (!quizzes.Any())
            {
                // Returnăm șir gol dacă nu sunt date
                return Ok(new List<object>());
            }

            const int maxDays = 30;
            var result = new List<object>(maxDays + 1);

            for (int day = 0; day <= maxDays; day++)
            {
                // Calculăm retention per quiz: exp(-day / stability)
                var retentions = quizzes
                    .Select(q => Math.Exp(-day / q.Stability))
                    .ToList();

                // Medie și procent
                double avgRetention = retentions.Average() * 100.0;
                avgRetention = Math.Round(avgRetention, 2);

                result.Add(new { day, retention = avgRetention });
            }

            return Ok(result);
        }






    }
}
