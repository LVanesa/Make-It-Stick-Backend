namespace Make_It_Stick_Backend.Models.Requests
{
    public class CreateQuizQuestionRequest
    {
        public int NoteId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }

}
