using System.Text.Json.Serialization;

namespace Make_It_Stick_Backend.Models.Dto
{
    public class AddToUserLibraryDto
    {
        public string GoogleBookId { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LibraryShelf LibraryShelf { get; set; }
    }
}
