namespace Make_It_Stick_Backend.Models
{
    public class FlashcardNote : BaseNote
    {
        public string FlashcardFront { get; set; } = default!;
        public string FlashcardBack { get; set; } = default!;
    }
}
