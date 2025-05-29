using System.Text.Json.Serialization;

namespace Make_It_Stick_Backend.Models.Requests
{
    public class FSRSReviewResponse
    {
        [JsonPropertyName("interval")]
        public int Interval { get; set; }

        [JsonPropertyName("stability")]
        public double Stability { get; set; }

        [JsonPropertyName("difficulty")]
        public double Difficulty { get; set; }

        [JsonPropertyName("next_review")]
        public DateTime NextReview { get; set; }
    }
}
