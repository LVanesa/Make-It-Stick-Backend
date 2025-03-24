using System.Collections.Generic;
using Newtonsoft.Json;

namespace Make_It_Stick_Backend.Models.Response
{
    public class NYTBooksResponseDto
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("num_results")]
        public int NumResults { get; set; }

        [JsonProperty("results")]
        public NYTBooksResultsDto Results { get; set; }
    }

    public class NYTBooksResultsDto
    {
        [JsonProperty("list_name")]
        public string ListName { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        [JsonProperty("bestsellers_date")]
        public string BestsellersDate { get; set; }

        [JsonProperty("published_date")]
        public string PublishedDate { get; set; }

        [JsonProperty("books")]
        public List<NYTBookDto> Books { get; set; }
    }

    public class NYTBookDto
    {
        [JsonProperty("isbns")]
        public List<NYTIsbnDto> Isbns { get; set; }
    }

    public class NYTIsbnDto
    {
        [JsonProperty("isbn10")]
        public string Isbn10 { get; set; }

        [JsonProperty("isbn13")]
        public string Isbn13 { get; set; }
    }
}
public class MergedNYTBooksDto
{
    public List<string> Isbn10List { get; set; }
    public List<string> Isbn13List { get; set; }
}
