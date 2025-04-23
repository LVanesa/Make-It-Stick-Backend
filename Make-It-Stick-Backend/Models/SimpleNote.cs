namespace Make_It_Stick_Backend.Models
{
    public class SimpleNote : BaseNote
    {
        public string Title { get; set; } = default!;
        public string Content { get; set; } = default!;
    }
}
