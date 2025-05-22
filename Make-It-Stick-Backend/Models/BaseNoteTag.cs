namespace Make_It_Stick_Backend.Models
{
    public class BaseNoteTag
    {
        public int NoteId { get; set; }
        public BaseNote Note { get; set; } = default!;

        public int TagId { get; set; }
        public Tag Tag { get; set; } = default!;
    }
}
