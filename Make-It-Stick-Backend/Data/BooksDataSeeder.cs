using Make_It_Stick_Backend.Helpers;
using Make_It_Stick_Backend.Models;
using Make_It_Stick_Backend.Services;
using Newtonsoft.Json;

namespace Make_It_Stick_Backend.Data
{
    public class BooksDataSeeder
    {
        private readonly AppDbContext _context;
        private readonly GoogleBooksService _googleBooksService;
        private readonly string _dataFolder;

        public BooksDataSeeder(AppDbContext context, GoogleBooksService googleBooksService)
        {
            _context = context;
            _googleBooksService = googleBooksService;
            // Folderul unde sunt stocate fișierele JSON (ex: "Data")
            _dataFolder = Path.Combine(AppContext.BaseDirectory, "Data");
        }



        public async Task SeedBooksAsync()
        {
            // Dacă baza de date conține deja cărți, nu se face seeding-ul
            if (_context.Books.Any())
            {
                Console.WriteLine("Books already seeded.");
                return;
            }

            string jsonFilePath = Path.Combine(_dataFolder, "trending_books_isbn.json");
            if (!File.Exists(jsonFilePath))
            {
                Console.WriteLine("Trending books ISBN file not found.");
                return;
            }

            Dictionary<string, List<string>> genreIsbnData;

            try
            {
                string jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                genreIsbnData = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(jsonContent);
                if (genreIsbnData == null || genreIsbnData.Count == 0)
                {
                    Console.WriteLine("No data in trending books ISBN file.");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading ISBN file: {ex.Message}");
                return;
            }

            List<Book> booksToAdd = new List<Book>();

            // Parcurgem fiecare gen (cheie) și lista de ISBN-uri asociată



            foreach (var kvp in genreIsbnData)
            {

                string listName = kvp.Key; // Numele listei de bestseller-uri
                foreach (var isbn in kvp.Value.Distinct())
                {
                    try
                    {
                        // Efectuează apelul către Google Books API pentru a obține detaliile cărții pentru fiecare ISBN
                        var googleResponse = await _googleBooksService.SearchBooksByISBNAsync($"isbn:{isbn}", "relevance");
                        if (googleResponse?.Items != null && googleResponse.Items.Any())
                        {
                            var item = googleResponse.Items.First();
                            var volume = item.VolumeInfo;

                            // Extrage ISBN-13 din IndustryIdentifiers (dacă există)
                            string isbn13 = null;
                            if (volume.IndustryIdentifiers != null)
                            {
                                isbn13 = volume.IndustryIdentifiers.FirstOrDefault(i => i.Type.Equals("ISBN_13", StringComparison.OrdinalIgnoreCase))?.Identifier;

                            }

                            // Dacă nu a fost găsit ISBN13, sari peste această carte
                            if (string.IsNullOrWhiteSpace(isbn13))
                            {
                                Console.WriteLine($"Skipping ISBN {isbn} because ISBN13 is null.");
                                continue;
                            }

                            var book = new Book
                            {
                                GoogleBookId = item.Id,
                                Title = volume.Title,
                                Authors = volume.Authors != null ? string.Join(", ", volume.Authors) : null,
                                CoverUrl = volume.ImageLinks?.Thumbnail,
                                ISBN13 = isbn13,
                                Description = volume.Description,
                                PageCount = volume.PageCount,
                                AverageRating = volume.AverageRating,
                                PublishedDate = volume.PublishedDate,
                                // Dacă volume.Categories este disponibil, îl folosim; altfel, se folosește numele listei
                                Categories = (volume.Categories != null && volume.Categories.Any())
                                                ? string.Join(", ", volume.Categories)
                                                : listName,
                                AboutAuthor = null, 
                                Genre = listName  // Salvează numele listei din care provine cartea
                            };

                            // Adaugă cartea doar dacă nu există deja în lista locală (după GoogleBookId)
                            if (!booksToAdd.Any(b => b.GoogleBookId == book.GoogleBookId))
                            {
                                booksToAdd.Add(book);
                                Console.WriteLine($"Added book: {book.Title} (List: {listName})");
                            }
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing ISBN {isbn}: {ex.Message}");
                    }
                }

            }

            if (booksToAdd.Any())
            {
                _context.Books.AddRange(booksToAdd);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Seeded {booksToAdd.Count} books.");
            }
            else
            {
                Console.WriteLine("No books were seeded.");
            }
        }
    }
}
