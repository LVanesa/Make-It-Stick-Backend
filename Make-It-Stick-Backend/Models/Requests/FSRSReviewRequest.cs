using System.Text.Json.Serialization;

namespace Make_It_Stick_Backend.Models.Requests
{
    public class FSRSReviewRequest
    {
        [JsonPropertyName("difficulty")]
        public double Difficulty { get; set; }

        [JsonPropertyName("stability")]
        public double Stability { get; set; }

        [JsonPropertyName("last_review")]
        public DateTime LastReview { get; set; }

        [JsonPropertyName("rating")]
        public int Rating { get; set; }
    }
}
