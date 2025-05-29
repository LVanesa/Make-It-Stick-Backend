namespace Make_It_Stick_Backend.Models.Requests
{
    public class SubmitReviewRequest
    {
        public int QuestionId { get; set; }
        public ReviewRating Rating { get; set; }
    }

}
