namespace Make_It_Stick_Backend.Models.Dto
{
    public class PredictRequestDto
    {
        public string Text { get; set; }
    }

    public class SuggestionDto
    {
        public string TagId { get; set; }
        public string Name { get; set; }
    }
}
