namespace Make_It_Stick_Backend.Models.Dto
{
    public class DefinitionNoteDto
    {
        public string GoogleBookId { get; set; } = string.Empty;
        public string Word { get; set; } = default!;
        public string Definition { get; set; } = default!;
        public string? Example { get; set; }
        public bool IsFavorite { get; set; } = false;
        public List<int> TagIds { get; set; } = new();
    }

}
