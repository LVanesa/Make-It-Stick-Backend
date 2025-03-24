using Newtonsoft.Json;

public class GoogleBooksResponseDto
{
    [JsonProperty("items")]
    public List<GoogleBookItemDto> Items { get; set; }
}

public class GoogleBookItemDto
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("volumeInfo")]
    public VolumeInfoDto VolumeInfo { get; set; }
}

public class VolumeInfoDto
{
    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("authors")]
    public List<string> Authors { get; set; }

    [JsonProperty("imageLinks")]
    public ImageLinksDto ImageLinks { get; set; }

    [JsonProperty("industryIdentifiers")]
    public List<IndustryIdentifierDto> IndustryIdentifiers { get; set; }

    // Pentru detalii:
    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("pageCount")]
    public int? PageCount { get; set; }

    [JsonProperty("averageRating")]
    public double? AverageRating { get; set; }

    [JsonProperty("publishedDate")]
    public string PublishedDate { get; set; }

    [JsonProperty("categories")]
    public List<string> Categories { get; set; }
}

public class ImageLinksDto
{
    [JsonProperty("thumbnail")]
    public string Thumbnail { get; set; }
}

public class IndustryIdentifierDto
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("identifier")]
    public string Identifier { get; set; }
}

// DTO pentru detalii (poți reutiliza VolumeInfoDto pentru detalii dacă dorești)
public class GoogleBookDetailsDto
{
    [JsonProperty("volumeInfo")]
    public VolumeInfoDto VolumeInfo { get; set; }
}
