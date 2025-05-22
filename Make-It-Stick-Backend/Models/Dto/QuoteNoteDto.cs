namespace Make_It_Stick_Backend.Models.Dto
{
    public class QuoteNoteDto
    {

        public string GoogleBookId { get; set; } = string.Empty;

        public string Quote { get; set; } = default!;
        public string? Comment { get; set; }
        public bool IsFavorite { get; set; }
        public List<int> TagIds { get; set; } = new();
    }

}
