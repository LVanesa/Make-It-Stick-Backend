using Newtonsoft.Json;
using Make_It_Stick_Backend.Models.Response;

namespace Make_It_Stick_Backend.Services
{
    public class NYTBooksService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public NYTBooksService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["NYT:ApiKey"];
        }

        public async Task<NYTBooksResponseDto> GetBestsellersForListAsync(string date, string list)
        {
            var url = $"lists/{date}/{list}.json?api-key={_apiKey}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<NYTBooksResponseDto>(json);
        }

        public async Task<MergedNYTBooksDto> GetBestsellerIsbnsAsync(string date = "current", string? list = null)
        {
            var isbn10s = new List<string>();
            var isbn13s = new List<string>();

            // Funcție locală pentru procesarea unui răspuns
            void ProcessResponse(NYTBooksResponseDto? response)
            {
                if (response?.Results?.Books == null)
                    return;

                foreach (var book in response.Results.Books)
                {
                    if (book.Isbns != null)
                    {
                        bool isbnFound = false;
                        // Încearcă mai întâi să obții isbn13
                        foreach (var isbn in book.Isbns)
                        {
                            if (!string.IsNullOrWhiteSpace(isbn.Isbn13))
                            {
                                isbn13s.Add(isbn.Isbn13);
                                isbnFound = true;
                                break; // Adaugă un singur ISBN pentru fiecare carte
                            }
                        }
                        // Dacă nu s-a găsit isbn13, caută isbn10
                        if (!isbnFound)
                        {
                            foreach (var isbn in book.Isbns)
                            {
                                if (!string.IsNullOrWhiteSpace(isbn.Isbn10))
                                {
                                    isbn10s.Add(isbn.Isbn10);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(list))
            {
                // Dacă parametrul "list" este specificat, folosește-l
                var response = await GetBestsellersForListAsync(date, list);
                ProcessResponse(response);
            }
            else
            {
                // Dacă "list" nu este specificat, combină cele două liste implicite
                string listFiction = "combined-print-and-e-book-fiction";
                string listNonfiction = "combined-print-and-e-book-nonfiction";

                var responseFiction = await GetBestsellersForListAsync(date, listFiction);
                var responseNonfiction = await GetBestsellersForListAsync(date, listNonfiction);

                ProcessResponse(responseFiction);
                ProcessResponse(responseNonfiction);
            }

            return new MergedNYTBooksDto
            {
                Isbn10List = isbn10s,
                Isbn13List = isbn13s
            };
        }


    }
}
