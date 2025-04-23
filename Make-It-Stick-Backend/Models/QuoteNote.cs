namespace Make_It_Stick_Backend.Models
{
    public class QuoteNote : BaseNote
    {
        public string Quote { get; set; } = default!;
        public string? comment { get; set; }
    }
}
