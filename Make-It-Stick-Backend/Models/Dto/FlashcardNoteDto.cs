namespace Make_It_Stick_Backend.Models.Dto
{
    public class FlashcardNoteDto
    {

        public string GoogleBookId { get; set; } = string.Empty;
        public string FlashcardFront { get; set; } = default!;
        public string FlashcardBack { get; set; } = default!;
        public bool IsFavorite { get; set; } = false;
        public List<int> TagIds { get; set; } = new();
    }

}
