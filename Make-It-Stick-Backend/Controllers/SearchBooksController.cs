using Make_It_Stick_Backend.Data;
using Make_It_Stick_Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;


namespace Make_It_Stick_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchBooksController : ControllerBase
    {
        private readonly GoogleBooksService _googleBooksService;
        private readonly AppDbContext _context;

        public SearchBooksController(GoogleBooksService googleBooksService, AppDbContext context)
        {
            _googleBooksService = googleBooksService;
            _context = context;
        }

        // Endpoint de căutare: primește un query și un criteriu de sortare
        // Răspunsul va conține doar câmpurile esențiale (ID, Titlu, Autori, Copertă și IndustryIdentifiers)
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string q, [FromQuery] string orderBy = "relevance")
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Query is required.");
            var result = await _googleBooksService.SearchBooksAsync(q, orderBy);
            return Ok(result);
        }

        // Endpoint pentru detalii: verifică mai întâi baza de date locală
        // Dacă cartea există deja, o returnează din baza locală,
        // altfel face request către Google Books API pentru a obține detaliile
        [HttpGet("{googleBookId}")]
        public async Task<IActionResult> GetBookDetails(string googleBookId)
        {
            if (string.IsNullOrWhiteSpace(googleBookId))
                return BadRequest("GoogleBookId is required.");

            // Caută cartea în baza de date locală
            var localBook = await _context.Books.FirstOrDefaultAsync(b => b.GoogleBookId == googleBookId);
            if (localBook != null)
            {
                Console.WriteLine("Book found in local database");
                return Ok(localBook);
            }

            // Dacă nu există în baza de date, apelează Google Books API pentru detalii
            var googleResponse = await _googleBooksService.GetBookDetailsAsync(googleBookId);
            if (googleResponse == null || googleResponse.VolumeInfo == null)
                return NotFound("Book details not found from Google Books API.");

            var volume = googleResponse.VolumeInfo;

            // Extrage ISBN-13 din industryIdentifiers dacă este disponibil
            string isbn13 = null;
            if (volume.IndustryIdentifiers != null)
            {
                isbn13 = volume.IndustryIdentifiers
                    .FirstOrDefault(i => i.Type.Equals("ISBN_13", StringComparison.OrdinalIgnoreCase))
                    ?.Identifier;
            }

            // Mapează datele de la Google Books API la formatul local
            var transformedBook = new
            {
                bookId = 0, // Poți seta 0 sau lăsa implicit, deoarece nu există în baza de date încă
                googleBookId = googleBookId,
                title = volume.Title ?? "No title",
                authors = volume.Authors != null ? string.Join(", ", volume.Authors) : "Unknown",
                coverUrl = volume.ImageLinks?.Thumbnail,
                isbN13 = isbn13,
                // Dacă dorești, poți folosi o funcție pentru a elimina tag-urile HTML din descriere:
                description = StripHtml(volume.Description) ?? "",
                pageCount = volume.PageCount,
                averageRating = volume.AverageRating,
                publishedDate = volume.PublishedDate,
                // Dacă volume.categories este un array, le poți uni într-un string
                categories = volume.Categories != null ? string.Join(", ", volume.Categories) : "",
                aboutAuthor = (string)null,
                // Poți seta genre la fel ca categories sau într-un alt mod
                // Poți seta genre la fel ca categories sau într-un alt mod
                genre = volume.Categories != null && volume.Categories.Count > 0
                            ? string.Join(", ", volume.Categories)
                            : "Unknown"
            };

            return Ok(transformedBook);
        }

        // Funcție helper pentru a elimina tag-urile HTML din descriere
        private string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return html;
            return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
        }

    }
}
