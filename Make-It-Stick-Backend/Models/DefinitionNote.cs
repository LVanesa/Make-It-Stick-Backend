namespace Make_It_Stick_Backend.Models
{
    public class DefinitionNote : BaseNote
    {
        public string Word { get; set; } = default!;
        public string? Definition { get; set; } = default!;
        public string? Example { get; set; }
    }
}
