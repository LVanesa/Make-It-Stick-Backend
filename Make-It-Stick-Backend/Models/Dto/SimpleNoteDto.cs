namespace Make_It_Stick_Backend.Models.Dto
{
    public class SimpleNoteDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsFavorite { get; set; }
        public string GoogleBookId { get; set; } = string.Empty; 
        public List<int> TagIds { get; set; } = new();           
    }


}