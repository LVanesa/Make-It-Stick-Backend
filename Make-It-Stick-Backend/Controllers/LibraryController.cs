using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Make_It_Stick_Backend.Data;
using Make_It_Stick_Backend.Helpers;
using Make_It_Stick_Backend.Models;
using Make_It_Stick_Backend.Models.Dto;
using Make_It_Stick_Backend.Services;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Microsoft.AspNetCore.Authorization;

namespace Make_It_Stick_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LibraryController : BaseController
    {
        

        private readonly GoogleBooksService _googleBooksService;


        public LibraryController(AppDbContext context, GoogleBooksService googleBooksService) : base(context) {
            _googleBooksService = googleBooksService;
        }


        // Funcție care returnează cărțile bestseller dintr-o anumită categorie (genre=categorie), sau returnează toate bestseller-urile dacă nu se specifică nicio categorie
        // GET: api/Library/Bestsellers?genre={genre}
        [HttpGet]
        [Route("Bestsellers")]
        public async Task<IActionResult> GetBestsellers([FromQuery] string genre)
        {
            IQueryable<Book> query = _context.Books;
            if (!string.IsNullOrWhiteSpace(genre))
            {
                if (genre.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(b => b.Genre != null);
                }
                else
                {
                    query = query.Where(b => b.Genre == genre);
                }
            }
            var bestsellers = await query.ToListAsync();
            return Ok(bestsellers);
        }

        

    [HttpGet("Counts")]
    public async Task<IActionResult> GetLibraryCounts()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return Unauthorized("User not authenticated or not found.");

        // Obține toate valorile posibile ale enum-ului LibraryShelf
        var allShelves = Enum.GetValues(typeof(LibraryShelf)).Cast<LibraryShelf>();

        // Grupează LibraryItems din DB după LibraryShelf pentru user-ul identificat
        var counts = await _context.LibraryItems
            .Where(li => li.UserId == user.UserId)
            .GroupBy(li => li.LibraryShelf)
            .Select(g => new {
                LibraryShelf = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        // Completează valorile lipsă cu count = 0
        var result = allShelves.Select(shelf => new {
            LibraryShelf = shelf,
            Count = counts.FirstOrDefault(x => x.LibraryShelf == shelf)?.Count ?? 0
        }).ToList();

        return Ok(result);
    }




    // GET: api/Library/{userId}
    // Returnează elementele din biblioteca unui utilizator, incluzând informațiile despre carte.
    // Dacă libraryShelf este specificat, se vor returna doar elementele din raftul respectiv;
    // altfel se vor returna toate elementele.
 

    [HttpGet]
    public async Task<IActionResult> GetUserLibrary([FromQuery] LibraryShelf? libraryShelf)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return Unauthorized("User not authenticated or not found.");

        // Construiește query-ul pentru LibraryItems aferente user-ului
        var query = _context.LibraryItems
        .Include(li => li.Book)
        .Where(li => li.UserId == user.UserId);

        // Dacă s-a specificat un LibraryShelf, filtrează rezultatul
        if (libraryShelf.HasValue)
        {
            query = query.Where(li => li.LibraryShelf == libraryShelf.Value);
        }

        var libraryItems = await query.ToListAsync();
        return Ok(libraryItems);
    }



    // POST: api/Library/SaveBook?googleBookId={googleBookId}
    // Primește un googleBookId, verifică dacă cartea există în Books; dacă nu, o preia din Google Books API și o salvează.
    [HttpPost("SaveBook")]
        public async Task<IActionResult> SaveBook([FromQuery] string googleBookId)
        {
            if (string.IsNullOrWhiteSpace(googleBookId))
                return BadRequest("GoogleBookId is required.");

            // Verifică dacă cartea există deja
            var existingBook = await _context.Books.FirstOrDefaultAsync(b => b.GoogleBookId == googleBookId);
            if (existingBook != null)
                return Ok(existingBook);

            var bookDetails = await _googleBooksService.GetBookDetailsAsync(googleBookId);
            if (bookDetails == null || bookDetails.VolumeInfo == null)
                return NotFound("Book details not found.");

            // Extrage ISBN-13 din industryIdentifiers, dacă există
            string isbn13 = null;
            if (bookDetails.VolumeInfo.IndustryIdentifiers != null)
            {
                isbn13 = bookDetails.VolumeInfo.IndustryIdentifiers
                    .FirstOrDefault(i => i.Type.Equals("ISBN_13", StringComparison.OrdinalIgnoreCase))
                    ?.Identifier;
            }

            var book = new Book
            {
                GoogleBookId = googleBookId,
                Title = bookDetails.VolumeInfo.Title,
                Authors = bookDetails.VolumeInfo.Authors != null ? string.Join(", ", bookDetails.VolumeInfo.Authors) : null,
                CoverUrl = bookDetails.VolumeInfo.ImageLinks?.Thumbnail,
                ISBN13 = isbn13,
                Description = TextCleaner.CleanText(bookDetails.VolumeInfo.Description),
                PageCount = bookDetails.VolumeInfo.PageCount,
                AverageRating = bookDetails.VolumeInfo.AverageRating,
                PublishedDate = bookDetails.VolumeInfo.PublishedDate,
                Categories = bookDetails.VolumeInfo.Categories != null ? string.Join(", ", bookDetails.VolumeInfo.Categories) : null,
                
            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync();
            return Ok(book);
        }

        [HttpDelete("DeleteLibraryItem/{googleBookId}")]
        public async Task<IActionResult> DeleteBookFromUserLibrary(string googleBookId)
        {
            if (string.IsNullOrWhiteSpace(googleBookId))
                return BadRequest("GoogleBookId is required.");

            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            // Găsește LibraryItem pentru cartea dată, care aparține utilizatorului curent.
            var libraryItem = await _context.LibraryItems
                .Include(li => li.Book)
                .FirstOrDefaultAsync(li => li.UserId == user.UserId && li.Book.GoogleBookId == googleBookId);

            if (libraryItem == null)
                return NotFound("Library item not found.");

            _context.LibraryItems.Remove(libraryItem);
            await _context.SaveChangesAsync();

            return Ok($"Book with GoogleBookId {googleBookId} removed from user's library successfully.");
        }


        [HttpPost("AddToUserLibrary")]
        public async Task<IActionResult> AddToUserLibrary([FromBody] AddToUserLibraryDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            // Verifică dacă cartea există; dacă nu, o adaugă folosind metoda SaveBook.
            var book = await _context.Books.FirstOrDefaultAsync(b => b.GoogleBookId == dto.GoogleBookId);
            if (book == null)
            {
                var bookDetails = await _googleBooksService.GetBookDetailsAsync(dto.GoogleBookId);
                if (bookDetails == null || bookDetails.VolumeInfo == null)
                    return NotFound("Book details not found.");

                string isbn13 = null;
                if (bookDetails.VolumeInfo.IndustryIdentifiers != null)
                {
                    isbn13 = bookDetails.VolumeInfo.IndustryIdentifiers
                        .FirstOrDefault(i => i.Type.Equals("ISBN_13", StringComparison.OrdinalIgnoreCase))
                        ?.Identifier;
                }

                book = new Book
                {
                    GoogleBookId = dto.GoogleBookId,
                    Title = bookDetails.VolumeInfo.Title,
                    Authors = bookDetails.VolumeInfo.Authors != null ? string.Join(", ", bookDetails.VolumeInfo.Authors) : null,
                    CoverUrl = bookDetails.VolumeInfo.ImageLinks?.Thumbnail,
                    ISBN13 = isbn13,
                    Description = TextCleaner.CleanText(bookDetails.VolumeInfo.Description),
                    PageCount = bookDetails.VolumeInfo.PageCount,
                    AverageRating = bookDetails.VolumeInfo.AverageRating,
                    PublishedDate = bookDetails.VolumeInfo.PublishedDate,
                    Categories = bookDetails.VolumeInfo.Categories != null ? string.Join(", ", bookDetails.VolumeInfo.Categories) : null,
                };

                _context.Books.Add(book);
                await _context.SaveChangesAsync();
            }

            // Obține bookId-ul din tabelul Books
            int bookId = await _context.Books
                .Where(b => b.GoogleBookId == dto.GoogleBookId)
                .Select(b => b.BookId)
                .FirstOrDefaultAsync();

            // Verifică dacă există deja un LibraryItem pentru această carte a utilizatorului
            var existingLibraryItem = await _context.LibraryItems
                .FirstOrDefaultAsync(li => li.UserId == user.UserId && li.BookId == bookId);

            if (existingLibraryItem != null)
            {
                // Dacă raftul este deja același, returnează o eroare
                if (existingLibraryItem.LibraryShelf == dto.LibraryShelf)
                {
                    return BadRequest("Book already exists in the user's library on this shelf.");
                }
                else
                {
                    // Actualizează raftul și datele corespunzătoare
                    existingLibraryItem.LibraryShelf = dto.LibraryShelf;
                    // Resetează DateStarted sau DateFinished în funcție de noul raft
                    if (dto.LibraryShelf == LibraryShelf.CurrentlyReading)
                    {
                        existingLibraryItem.DateStarted = DateTime.UtcNow.Date;
                        // Opțional, poți șterge DateFinished dacă era setat anterior
                        existingLibraryItem.DateFinished = null;
                    }
                    else if (dto.LibraryShelf == LibraryShelf.Finished || dto.LibraryShelf == LibraryShelf.Abandoned)
                    {
                        existingLibraryItem.DateFinished = DateTime.UtcNow.Date;
                    }
                    await _context.SaveChangesAsync();
                    return Ok(existingLibraryItem);
                }
            }

            // Dacă nu există LibraryItem, creează unul nou
            var libraryItem = new LibraryItem
            {
                UserId = user.UserId,  // Folosește user.UserId extras din token
                BookId = bookId,
                LibraryShelf = dto.LibraryShelf,
                DateAdded = DateTime.UtcNow.Date,
            };

            if (dto.LibraryShelf == LibraryShelf.CurrentlyReading)
            {
                libraryItem.DateStarted = DateTime.UtcNow.Date;
            }
            else if (dto.LibraryShelf == LibraryShelf.Finished || dto.LibraryShelf == LibraryShelf.Abandoned)
            {
                libraryItem.DateFinished = DateTime.UtcNow.Date;
            }

            _context.LibraryItems.Add(libraryItem);
            await _context.SaveChangesAsync();

            return Ok(libraryItem);
        }


        [HttpGet("Item")]
        public async Task<IActionResult> GetLibraryItemByGoogleBookId([FromQuery] string googleBookId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Unauthorized("User not authenticated or not found.");

            var libraryItem = await _context.LibraryItems
                .Include(li => li.Book)
                .FirstOrDefaultAsync(li => li.UserId == user.UserId && li.Book.GoogleBookId == googleBookId);

            if (libraryItem == null)
                return NotFound("Library item not found.");

            return Ok(libraryItem);
        }

        [Authorize]
        [HttpGet("currently-reading")]
        public async Task<IActionResult> GetCurrentlyReading()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized("User not authenticated or not found.");

            var items = await _context.LibraryItems
                .Include(li => li.Book)
                .Where(li => li.UserId == user.UserId && li.LibraryShelf == LibraryShelf.CurrentlyReading)
                .Select(li => new {
                    li.LibraryItemId,
                    li.CurrentPage,
                    TotalPages = li.TotalPages ?? li.Book.PageCount,
                    BookId = li.Book.BookId,
                    GoogleBookId=li.Book.GoogleBookId,
                    Title = li.Book.Title,
                    Author = li.Book.Authors,
                    CoverUrl = li.Book.CoverUrl
                })
                .ToListAsync();

            return Ok(items);
        }


        [HttpPut("UpdateCurrentPage")]
        public async Task<IActionResult> UpdateCurrentPage([FromBody] UpdatePageDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized("User not authenticated");

            var item = await _context.LibraryItems
                .Include(li => li.Book)
                .FirstOrDefaultAsync(li => li.UserId == user.UserId && li.Book.GoogleBookId == dto.GoogleBookId);

            if (item == null) return NotFound("Library item not found");

            item.CurrentPage = dto.NewValue;
            await _context.SaveChangesAsync();

            return Ok(item);
        }

        [HttpPut("UpdateTotalPages")]
        public async Task<IActionResult> UpdateTotalPages([FromBody] UpdatePageDto dto)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized("User not authenticated");

            var item = await _context.LibraryItems
                .Include(li => li.Book)
                .FirstOrDefaultAsync(li => li.UserId == user.UserId && li.Book.GoogleBookId == dto.GoogleBookId);

            if (item == null) return NotFound("Library item not found");

            item.TotalPages = dto.NewValue;
            await _context.SaveChangesAsync();

            return Ok(item);
        }

        [HttpPut("UpdateShelf")]
        public async Task<IActionResult> UpdateShelf([FromBody] UpdateShelfDto dto)
        {
            if (string.IsNullOrEmpty(dto.GoogleBookId))
                return BadRequest("GoogleBookId is required.");

            // 1. Găsește userul folosind token-ul
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized("User not authenticated");


            // 2. Găsește cartea
            var book = await _context.Books.FirstOrDefaultAsync(b => b.GoogleBookId == dto.GoogleBookId);
            if (book == null)
                return NotFound("Book not found.");

            // 3. Găsește LibraryItem pentru user și carte
            var libraryItem = await _context.LibraryItems
                .FirstOrDefaultAsync(li => li.UserId == user.UserId && li.BookId == book.BookId);

            if (libraryItem == null)
                return NotFound("Library item not found.");

            // 4. Update la raft și dată corespunzătoare
            libraryItem.LibraryShelf = dto.LibraryShelf;

            if (dto.LibraryShelf == LibraryShelf.CurrentlyReading)
            {
                libraryItem.DateStarted = DateTime.UtcNow;
                libraryItem.DateFinished = null;
            }
            else if (dto.LibraryShelf == LibraryShelf.Finished || dto.LibraryShelf == LibraryShelf.Abandoned)
            {
                libraryItem.DateFinished = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(libraryItem);
        }


        // Adaugă această metodă în LibraryController.cs

        [HttpGet]
        [Route("Recommendations")]
        public async Task<IActionResult> GetRecommendations(
            [FromQuery] string query,
            [FromQuery] string? category = null,
            [FromQuery] string? mood = null,
            [FromQuery] int top_k = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query parameter is required.");

            try
            {
                using var httpClient = new HttpClient();

                // Construiește request-ul către FastAPI
                var requestBody = new
                {
                    query = query.Trim(),
                    category = category == "All" ? null : category,
                    tone = mood == "All" ? null : mood,
                    top_k = top_k
                };

                var jsonContent = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(requestBody),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                // Apelează FastAPI pentru a obține ISBN-urile
                var fastApiResponse = await httpClient.PostAsync("http://127.0.0.1:8000/recommend/isbn", jsonContent);

                if (!fastApiResponse.IsSuccessStatusCode)
                {
                    var errorContent = await fastApiResponse.Content.ReadAsStringAsync();
                    return StatusCode((int)fastApiResponse.StatusCode,
                        $"FastAPI error: {errorContent}");
                }

                var fastApiResult = await fastApiResponse.Content.ReadAsStringAsync();
                var isbnResponse = System.Text.Json.JsonSerializer.Deserialize<FastApiIsbnResponse>(fastApiResult);

                if (isbnResponse?.isbn_list == null || !isbnResponse.isbn_list.Any())
                {
                    return Ok(new List<object>()); // Returnează listă goală dacă nu sunt recomandări
                }

                // Convertește ISBN-urile în cărți complete folosind Google Books API
                var books = new List<Book>();

                foreach (var isbn in isbnResponse.isbn_list.Take(top_k))
                {
                    try
                    {
                        // Folosește serviciul existent pentru căutare după ISBN
                        var searchResults = await _googleBooksService.SearchBooksByISBNAsync($"isbn:{isbn}");

                        if (searchResults?.Items != null && searchResults.Items.Any())
                        {
                            var bookItem = searchResults.Items.First();

                            // Verifică dacă cartea există deja în baza de date
                            var existingBook = await _context.Books
                                .FirstOrDefaultAsync(b => b.GoogleBookId == bookItem.Id);

                            Book book;
                            if (existingBook != null)
                            {
                                book = existingBook;
                            }
                            else
                            {
                                // Extrage ISBN-13 din industryIdentifiers
                                string isbn13 = isbn; // Default la ISBN-ul primit
                                if (bookItem.VolumeInfo?.IndustryIdentifiers != null)
                                {
                                    var isbn13Identifier = bookItem.VolumeInfo.IndustryIdentifiers
                                        .FirstOrDefault(i => i.Type?.Equals("ISBN_13", StringComparison.OrdinalIgnoreCase) == true);
                                    if (isbn13Identifier != null)
                                        isbn13 = isbn13Identifier.Identifier;
                                }

                                // Creează cartea nouă dar nu o salvează în DB (doar pentru recomandări)
                                book = new Book
                                {
                                    GoogleBookId = bookItem.Id,
                                    Title = bookItem.VolumeInfo?.Title ?? "Unknown Title",
                                    Authors = bookItem.VolumeInfo?.Authors != null
                                        ? string.Join(", ", bookItem.VolumeInfo.Authors)
                                        : "Unknown Author",
                                    CoverUrl = bookItem.VolumeInfo?.ImageLinks?.Thumbnail,
                                    ISBN13 = isbn13,
                                    Description = TextCleaner.CleanText(bookItem.VolumeInfo?.Description),
                                    PageCount = bookItem.VolumeInfo?.PageCount,
                                    AverageRating = bookItem.VolumeInfo?.AverageRating,
                                    PublishedDate = bookItem.VolumeInfo?.PublishedDate,
                                    Categories = bookItem.VolumeInfo?.Categories != null
                                        ? string.Join(", ", bookItem.VolumeInfo.Categories)
                                        : null,
                                };
                            }

                            books.Add(book);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error dar continuă cu următoarea carte
                        Console.WriteLine($"Error processing ISBN {isbn}: {ex.Message}");
                    }
                }

                return Ok(books);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Clasă pentru deserializarea răspunsului de la FastAPI
        public class FastApiIsbnResponse
        {
            public List<string> isbn_list { get; set; } = new List<string>();
            public int total_found { get; set; }
            public string query { get; set; } = string.Empty;
            public string? category { get; set; }
            public string? tone { get; set; }
            public string? message { get; set; }
        }







    }
}
