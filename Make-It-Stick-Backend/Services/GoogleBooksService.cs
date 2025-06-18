using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Make_It_Stick_Backend.Models.Response;

namespace Make_It_Stick_Backend.Services
{
    public class GoogleBooksService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly IMemoryCache _cache;

        public GoogleBooksService(HttpClient httpClient, IConfiguration configuration, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GoogleBooks:ApiKey"];
            _cache = cache;
        }

        /// <summary>
        /// Caută cărți pe baza unui query și returnează doar câmpurile esențiale:
        /// Id, Titlu, Autori, Imagine (copertă) și IndustryIdentifiers (din care extrag doar ISBN-13).
        /// </summary>
        /// 
        public async Task<GoogleBooksResponseDto> SearchBooksAsync(string query, string orderBy = "relevance")
        {
            // Solicităm doar câmpurile esențiale folosind parametrul "fields".
            string fields = "items(id,volumeInfo(title,authors,imageLinks/thumbnail,industryIdentifiers))";
            string cacheKey = $"GoogleBooks_Search_{query}_{orderBy}_{fields}";

            // Încercăm să obținem rezultatul din cache.
            if (_cache.TryGetValue(cacheKey, out GoogleBooksResponseDto cachedResult))
                return cachedResult;


            var url = $"volumes?q={Uri.EscapeDataString(query)}&orderBy={orderBy}&fields={Uri.EscapeDataString(fields)}&key={_apiKey}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<GoogleBooksResponseDto>(json);

            // Cache pentru 1 oră
            _cache.Set(cacheKey, result, TimeSpan.FromHours(1));
            return result;
        }
            
        public async Task<GoogleBooksResponseDto> SearchBooksByISBNAsync(string query, string orderBy = "relevance")
        {
            // Include industryIdentifiers pentru a putea extrage ISBN-13
            string fields = "items(id,volumeInfo(title,authors,description,pageCount,averageRating,publishedDate,categories,industryIdentifiers,imageLinks/thumbnail))";
            string cacheKey = $"GoogleBooks_Search_{query}_{orderBy}_{fields}";

            if (_cache.TryGetValue(cacheKey, out GoogleBooksResponseDto cachedResult))
                return cachedResult;

            var url = $"volumes?q={Uri.EscapeDataString(query)}&orderBy={orderBy}&fields={Uri.EscapeDataString(fields)}&key={_apiKey}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<GoogleBooksResponseDto>(json);

            _cache.Set(cacheKey, result, TimeSpan.FromHours(1));
            return result;
        }


        /// <summary>
        /// Obține detaliile complete ale unei cărți pe baza id-ului Google Books.
        /// Returnează doar câmpurile esențiale: Titlu, Autori, Descriere, Număr de pagini, Rating, Data publicării, ISBN-uri,
        /// Genuri și Imaginea de copertă.
        /// </summary>
        public async Task<GoogleBookDetailsDto> GetBookDetailsAsync(string googleBookId)
        {
            // Include industryIdentifiers pentru a putea extrage ISBN-13
            string fields = "volumeInfo(title,authors,description,pageCount,averageRating,publishedDate,categories,imageLinks/thumbnail,industryIdentifiers)";
            string cacheKey = $"GoogleBooks_Details_{googleBookId}_{fields}";
            if (_cache.TryGetValue(cacheKey, out GoogleBookDetailsDto cachedDetails))
                return cachedDetails;

            var url = $"volumes/{googleBookId}?fields={Uri.EscapeDataString(fields)}&key={_apiKey}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<GoogleBookDetailsDto>(json);

            // Cache pentru 1 oră
            _cache.Set(cacheKey, result, TimeSpan.FromHours(1));
            return result;
        }



    }
}
