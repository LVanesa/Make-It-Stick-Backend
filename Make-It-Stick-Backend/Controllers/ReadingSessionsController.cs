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
    public class ReadingSessionsController : BaseController
    {
        public ReadingSessionsController(AppDbContext context) : base(context) { }

        [HttpGet]
        public async Task<IActionResult> GetReadingSessions([FromQuery] string googleBookId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            if (string.IsNullOrWhiteSpace(googleBookId))
                return BadRequest("googleBookId is required.");

            // Căutăm LibraryItem-ul asociat userului și cărții
            var libraryItem = await _context.LibraryItems
                .Include(li => li.Book)
                .Include(li => li.ReadingSessions)
                .FirstOrDefaultAsync(li =>
                    li.Book.GoogleBookId == googleBookId &&
                    li.UserId == user.UserId);

            if (libraryItem == null)
                return NotFound("Library item not found for this user and book.");

            // Mapăm la un obiect mai simplu pentru frontend
            var sessionsDto = libraryItem.ReadingSessions
                .OrderBy(rs => rs.StartTime)
                .Select((session, index) => new
                {
                    SessionNumber = index + 1,
                    session.ReadingSessionId,
                    Date = session.StartTime,
                    StartTime = session.StartTime,
                    EndTime = session.EndTime,
                    session.StartPage,
                    session.EndPage,
                    session.PagesRead,
                    session.Duration
                })
                .ToList();

            return Ok(sessionsDto);
        }

        [HttpDelete("{readingSessionId}")]
        public async Task<IActionResult> DeleteReadingSession(int readingSessionId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            // Căutăm sesiunea și includem și LibraryItem pentru verificare user
            var session = await _context.ReadingSessions
                .Include(rs => rs.LibraryItem)
                .FirstOrDefaultAsync(rs => rs.ReadingSessionId == readingSessionId);

            if (session == null)
                return NotFound("Reading session not found.");

            if (session.LibraryItem.UserId != user.UserId)
                return Forbid("You do not have permission to delete this session.");

            _context.ReadingSessions.Remove(session);
            await _context.SaveChangesAsync();

            return NoContent();
        }



        [HttpPost("reading-session")]
        public async Task<IActionResult> SaveReadingSession([FromBody] ReadingSessionDto sessionDto)
        {
            // 1. Creează ReadingSession
            var session = new ReadingSession
            {
                LibraryItemId = sessionDto.LibraryItemId,
                StartTime = sessionDto.StartTime,
                EndTime = sessionDto.EndTime,
                StartPage = sessionDto.StartPage,
                EndPage = sessionDto.EndPage,
                Duration = (int)(sessionDto.EndTime - sessionDto.StartTime).TotalMinutes,
                PagesRead = sessionDto.EndPage - sessionDto.StartPage
            };
            _context.ReadingSessions.Add(session);

            // 2. Update LibraryItem
            var libraryItem = await _context.LibraryItems.FindAsync(sessionDto.LibraryItemId);
            if (libraryItem == null) return NotFound("LibraryItem not found");

            libraryItem.CurrentPage = sessionDto.EndPage;

            if (sessionDto.TotalPages.HasValue && sessionDto.TotalPages > 0)
            {
                if (libraryItem.TotalPages != sessionDto.TotalPages)
                {
                    libraryItem.TotalPages = sessionDto.TotalPages;
                }
            }

            // Dacă EngPage == TotalPages => marchez cartea ca finished
            if (libraryItem.TotalPages.HasValue && sessionDto.EndPage == libraryItem.TotalPages.Value)
            {  
                libraryItem.LibraryShelf = LibraryShelf.Finished;
                libraryItem.DateFinished = DateTime.UtcNow;
            }

            // 3. Update DailyProgress
            var userId = libraryItem.UserId;
            var today = sessionDto.StartTime.Date;

            var dailyProgress = await _context.DailyProgresses
                .FirstOrDefaultAsync(dp => dp.UserId == userId && dp.Date == today);

            int sessionDuration = session.Duration ?? 0;

            if (dailyProgress == null)
            {
                dailyProgress = new DailyProgress
                {
                    UserId = userId,
                    Date = today,
                    TotalMinutes = sessionDuration,
                    GoalReached = false, // îl actualizăm mai jos
                    LastUpdated = DateTime.UtcNow
                };
                _context.DailyProgresses.Add(dailyProgress);
            }
            else
            {
                dailyProgress.TotalMinutes += sessionDuration;
                dailyProgress.LastUpdated = DateTime.UtcNow;
            }

            // 4. Verifică dacă userul și-a atins goal-ul zilnic
            var user = await _context.Users
                .Include(u => u.DailyReadingGoal)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user?.DailyReadingGoal != null)
            {
                if (dailyProgress.TotalMinutes >= user.DailyReadingGoal.MinutesPerDay)
                {
                    dailyProgress.GoalReached = true;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Reading session saved successfully." });
        }




    }
}
