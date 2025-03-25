using Make_It_Stick_Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Make_It_Stick_Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing.Constraints;


namespace Make_It_Stick_Backend.Controllers
{

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class YearlyReadingGoalsController : BaseController
    {
        public YearlyReadingGoalsController(AppDbContext context) : base(context) { }


        // GET: api/YearlyReadingGoals?year=2025
        [HttpGet]
        public async Task<IActionResult> GetYearlyGoal([FromQuery] int? year)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            int targetYear = year ?? DateTime.UtcNow.Year;

            var goal = await _context.YearlyReadingGoals
                                     .Include(g => g.Books)
                                     .FirstOrDefaultAsync(g => g.UserId == user.UserId && g.Year == targetYear);

            // Calculează mereu BooksRead actualizat
            var booksReadList = await _context.LibraryItems
                                    .Include(li => li.Book)
                                    .Where(li => li.UserId == user.UserId
                                                 && li.LibraryShelf == LibraryShelf.Finished
                                                 && li.DateFinished.HasValue
                                                 && li.DateFinished.Value.Year == targetYear)
                                    .Select(li => new
                                    {
                                        BookId = li.Book.BookId,
                                        GoogleBookId = li.Book.GoogleBookId,
                                        Title = li.Book.Title,
                                        CoverUrl = li.Book.CoverUrl,
                                        DateFinished = li.DateFinished
                                    })
                                    .ToListAsync();

            // Actualizează valoarea BooksRead în obiectul goal, dacă acesta există
            if (goal != null)
            {
                int actualBooksRead = booksReadList.Count;

                // dacă e diferit, actualizează în baza de date
                if (goal.BooksRead != actualBooksRead)
                {
                    goal.BooksRead = actualBooksRead;
                    goal.UpdatedAt = DateTime.UtcNow;
                    _context.YearlyReadingGoals.Update(goal);
                    await _context.SaveChangesAsync();
                }
            }

            var response = new
            {
                Year = targetYear,
                Goal = goal != null ? new
                {
                    goal.Id,
                    goal.TargetBooks,
                    BooksRead = booksReadList.Count, // Folosește direct numărul calculat
                    goal.CreatedAt
                } : null,
                Books = booksReadList
            };

            return Ok(response);
        }




        // POST: api/YearlyReadingGoals -> adaugă un obiect YearlyReadingGoal pentru anul curent
        [HttpPost]
        public async Task<IActionResult> CreateYearlyGoal([FromBody] int no_books)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            var userId = user.UserId;
            var currentYear = DateTime.UtcNow.Year;

            // verifică dacă deja există goal pentru anul curent
            var exists = await _context.YearlyReadingGoals
                                 .AnyAsync(g => g.UserId == userId && g.Year == currentYear);

            if (exists)
                return BadRequest($"Goal for year {currentYear} already exists.");

            // Calculează câte cărți a citit deja userul în anul curent
            var booksReadCount = await _context.LibraryItems
                                       .CountAsync(li => li.UserId == userId
                                                      && li.LibraryShelf == LibraryShelf.Finished
                                                      && li.DateFinished.HasValue
                                                      && li.DateFinished.Value.Year == currentYear);

            // Creează noul obiectiv
            var newGoal = new YearlyReadingGoal
            {
                UserId = userId,
                Year = currentYear,
                TargetBooks = no_books,
                BooksRead = booksReadCount,
                CreatedAt = DateTime.UtcNow
            };

            _context.YearlyReadingGoals.Add(newGoal);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetYearlyGoal), new { year = currentYear }, newGoal);
        }

        // PUT: api/YearlyReadingGoals
        [HttpPut]
        public async Task<IActionResult> UpdateYearlyGoal([FromBody] int no_books)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            var currentYear = DateTime.UtcNow.Year;

            var goal = await _context.YearlyReadingGoals
                                     .FirstOrDefaultAsync(g => g.UserId == user.UserId && g.Year == currentYear);

            if (goal == null)
                return NotFound($"Goal for year {currentYear} not found.");

            goal.TargetBooks = no_books;
            goal.UpdatedAt = DateTime.UtcNow;

            _context.YearlyReadingGoals.Update(goal);
            await _context.SaveChangesAsync();

            return Ok(goal);
        }

        // GET: api/YearlyReadingGoals/history
        [HttpGet("history")]
        public async Task<IActionResult> GetYearlyGoalsHistory()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            var currentYear = DateTime.UtcNow.Year;

            var goals = await _context.YearlyReadingGoals
                                      .Where(g => g.UserId == user.UserId && g.Year < currentYear)
                                      .OrderByDescending(g => g.Year)
                                      .ToListAsync();

            var history = new List<object>();

            foreach (var goal in goals)
            {
                var booksReadCount = await _context.LibraryItems
                                           .CountAsync(li => li.UserId == user.UserId
                                                          && li.LibraryShelf == LibraryShelf.Finished
                                                          && li.DateFinished.HasValue
                                                          && li.DateFinished.Value.Year == goal.Year);

                history.Add(new
                {
                    goal.Year,
                    goal.TargetBooks,
                    BooksRead = booksReadCount
                });
            }

            return Ok(history);
        }

        // DELETE: api/YearlyReadingGoals?year=2024
        [HttpDelete]
        public async Task<IActionResult> DeleteYearlyGoal([FromQuery] int? year)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            var targetYear = year ?? DateTime.UtcNow.Year;

            var goal = await _context.YearlyReadingGoals
                                     .FirstOrDefaultAsync(g => g.UserId == user.UserId && g.Year == targetYear);

            if (goal == null)
                return NotFound($"Goal for year {targetYear} not found.");

            _context.YearlyReadingGoals.Remove(goal);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Goal for year {targetYear} deleted." });
        }












    }
}
