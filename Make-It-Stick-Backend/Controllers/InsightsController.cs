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

            //FIRST: Check if user has a yearly reading goal
            var goal = await _context.YearlyReadingGoals
                .Where(g => g.UserId == user.UserId && g.Year == currentYear)
                .FirstOrDefaultAsync();

            //If no goal exists, return early with appropriate message
            if (goal == null)
            {
                return Ok(new
                {
                    hasGoal = false,
                    currentYear = currentYear,
                    message = "Set your yearly reading goal to start tracking your progress"
                });
            }

            //GOAL EXISTS: Calculate all statistics
            var today = DateTime.UtcNow;
            var daysInYear = DateTime.IsLeapYear(today.Year) ? 366 : 365;
            var dayOfYear = today.DayOfYear;

            // Safe calculation - ensure goal > 0
            var progressPercent = (double)dayOfYear / daysInYear;
            var expectedBooksByNow = goal.TargetBooks > 0
                ? Math.Round(goal.TargetBooks * progressPercent, 2)
                : 0;

            // Count finished books for current year
            var booksFinished = await _context.LibraryItems
                .Where(b => b.UserId == user.UserId
                    && b.LibraryShelf == LibraryShelf.Finished
                    && b.DateFinished.HasValue
                    && b.DateFinished.Value.Year == currentYear)
                .CountAsync();

            // Safe difference calculation
            var difference = Math.Round(booksFinished - expectedBooksByNow, 2);

            // Improved pace status logic
            var paceStatus = difference switch
            {
                > 0.5 => "ahead",
                >= -0.5 and <= 0.5 => "on-track",
                _ => "behind"
            };

            // Calculate monthly breakdown for enhanced statistics
            var monthlyData = new List<object>();
            for (int month = 1; month <= 12; month++)
            {
                var monthlyTarget = Math.Round((double)goal.TargetBooks / 12 * month, 1);

                // Count books finished up to this month
                var booksFinishedUpToMonth = await _context.LibraryItems
                    .Where(b => b.UserId == user.UserId
                        && b.LibraryShelf == LibraryShelf.Finished
                        && b.DateFinished.HasValue
                        && b.DateFinished.Value.Year == currentYear
                        && b.DateFinished.Value.Month <= month)
                    .CountAsync();

                monthlyData.Add(new
                {
                    month = month,
                    target = monthlyTarget,
                    actual = month <= today.Month ? booksFinishedUpToMonth : (int?)null,
                    isCurrentMonth = month == today.Month,
                    isPastMonth = month < today.Month
                });
            }

            // Calculate reading pace metrics
            var currentMonth = today.Month;
            var monthsElapsed = currentMonth;
            var currentPace = monthsElapsed > 0 ? Math.Round((double)booksFinished / monthsElapsed, 2) : 0;

            var monthsRemaining = 12 - currentMonth;
            var booksRemaining = Math.Max(0, goal.TargetBooks - booksFinished);
            var requiredPace = monthsRemaining > 0 ? Math.Round((double)booksRemaining / monthsRemaining, 2) : 0;

            // Overall progress percentage
            var overallProgress = goal.TargetBooks > 0
                ? Math.Round((double)booksFinished / goal.TargetBooks * 100, 1)
                : 0;

            // Time-based expectations
            var expectedProgress = Math.Round(progressPercent * 100, 1); // Percentage of year elapsed

            return Ok(new
            {
                hasGoal = true,
                yearGoal = goal.TargetBooks,
                currentYear = currentYear,
                booksFinished = booksFinished,
                expectedBooksByNow = expectedBooksByNow,
                paceStatus = paceStatus,
                difference = difference,

                // Enhanced metrics
                monthlyData = monthlyData,
                currentPace = currentPace,
                requiredPace = requiredPace,
                overallProgress = overallProgress,
                expectedProgress = expectedProgress,

                // Additional insights
                monthsElapsed = monthsElapsed,
                monthsRemaining = monthsRemaining,
                booksRemaining = booksRemaining,

                // Status flags
                isAhead = difference > 0.5,
                isBehind = difference < -0.5,
                isOnTrack = difference >= -0.5 && difference <= 0.5
            });
        }

        // BONUS: Endpoint pentru yearly goals management
        [HttpGet("yearly-goals")]
        [Authorize]
        public async Task<IActionResult> GetYearlyGoals()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            var goals = await _context.YearlyReadingGoals
                .Where(g => g.UserId == user.UserId)
                .OrderByDescending(g => g.Year)
                .ToListAsync();

            var currentYear = DateTime.UtcNow.Year;
            var hasCurrentYearGoal = goals.Any(g => g.Year == currentYear);

            return Ok(new
            {
                hasCurrentYearGoal = hasCurrentYearGoal,
                currentYear = currentYear,
                goals = goals.Select(g => new
                {
                    g.Id,
                    g.Year,
                    g.TargetBooks,
                    g.CreatedAt
                }).ToList()
            });
        }

       

        [HttpGet("daily-reading-progress")]
        public async Task<IActionResult> GetDailyReadingProgress()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            // FIRST: Check if user has a daily reading goal
            var goalEntity = await _context.DailyReadingGoals.FirstOrDefaultAsync(g => g.UserId == user.UserId);

            // If no goal exists, return early with appropriate message
            if (goalEntity == null)
            {
                return Ok(new
                {
                    hasGoal = false,
                    message = "Set your daily reading goal to start tracking your progress"
                });
            }

            int goal = goalEntity.MinutesPerDay;
            var today = DateTime.UtcNow.Date;

            // Start of week = Monday
            var diff = ((int)today.DayOfWeek + 6) % 7;
            var startOfWeek = today.AddDays(-diff);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

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
                    completed = actual >= goal && goal > 0 // 🔥 Only completed if goal > 0
                };
            }).ToList();

            // Monthly stats - Fixed calculations
            int daysCompleted = goal > 0 ? dailyProgresses.Count(dp => dp.TotalMinutes >= goal) : 0;
            int totalDays = (today - startOfMonth).Days + 1;
            int activeDays = dailyProgresses.Count(dp => dp.TotalMinutes > 0);
            int totalMinutesRead = dailyProgresses.Sum(dp => dp.TotalMinutes);

            // Only calculate averages if there are active days
            int avgDailyAllDays = totalDays > 0 ? totalMinutesRead / totalDays : 0;
            int avgDailyActiveDays = activeDays > 0 ? totalMinutesRead / activeDays : 0;

            // Calculate productivity breakdown only if there are sessions
            var productivityBreakdown = new { morning = 0, afternoon = 0, evening = 0 };
            string mostProductiveTime = "morning"; // default

            if (readingSessions.Any())
            {
                var morningMinutes = readingSessions
                    .Where(rs => rs.StartTime.Hour >= 6 && rs.StartTime.Hour < 12)
                    .Sum(rs => rs.Duration ?? 0);

                var afternoonMinutes = readingSessions
                    .Where(rs => rs.StartTime.Hour >= 12 && rs.StartTime.Hour < 18)
                    .Sum(rs => rs.Duration ?? 0);

                var eveningMinutes = readingSessions
                    .Where(rs => rs.StartTime.Hour >= 18 || rs.StartTime.Hour < 6)
                    .Sum(rs => rs.Duration ?? 0);

                productivityBreakdown = new
                {
                    morning = morningMinutes,
                    afternoon = afternoonMinutes,
                    evening = eveningMinutes
                };

                // Determine most productive time only if there's actual data
                var maxMinutes = Math.Max(morningMinutes, Math.Max(afternoonMinutes, eveningMinutes));
                if (maxMinutes > 0)
                {
                    if (afternoonMinutes == maxMinutes)
                        mostProductiveTime = "afternoon";
                    else if (eveningMinutes == maxMinutes)
                        mostProductiveTime = "evening";
                    // else stays "morning"
                }
            }

            // FIXED: Current streak calculation - only if goal exists and > 0
            int streak = 0;
            if (goal > 0)
            {
                for (int i = 0; i < 100; i++)
                {
                    var date = today.AddDays(-i);
                    var dp = dailyProgresses.FirstOrDefault(d => d.Date.Date == date);
                    if (dp != null && dp.TotalMinutes >= goal)
                        streak++;
                    else
                        break;
                }
            }

            // FIXED: Best streak calculation - only if goal exists
            int bestStreak = 0;
            if (goal > 0)
            {
                var allProgresses = await _context.DailyProgresses
                    .Where(dp => dp.UserId == user.UserId)
                    .OrderBy(dp => dp.Date)
                    .ToListAsync();

                int currentStreak = 0;
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
            }

            // Calculate reading pace only if there are valid sessions
            double averageReadingPace = 0;
            var sessionsWithPages = readingSessions
                .Where(rs => rs.PagesRead.HasValue && rs.Duration.HasValue &&
                            rs.PagesRead > 0 && rs.Duration > 0)
                .ToList();

            if (sessionsWithPages.Any())
            {
                averageReadingPace = sessionsWithPages
                    .Average(rs => (double)rs.PagesRead.Value / rs.Duration.Value);
            }

            // Return structured response with goal confirmation
            var result = new
            {
                hasGoal = true,
                todayGoal = goal,
                todayProgress,
                streak,
                weeklyData,
                mostProductiveTime,
                productivityBreakdown,
                monthlyStats = new
                {
                    daysCompleted,
                    totalDays,
                    averageDaily = avgDailyActiveDays,
                    averageDailyAllDays = avgDailyAllDays,
                    bestStreak,
                    currentMonth = today.ToString("MMMM"),
                    totalSessions = readingSessions.Count,
                    averageReadingPace = Math.Round(averageReadingPace, 2),
                    // Additional helpful flags
                    hasAnyProgress = dailyProgresses.Any(),
                    hasAnySessions = readingSessions.Any()
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
