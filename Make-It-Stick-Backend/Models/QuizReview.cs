using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Make_It_Stick_Backend.Models
{
    public class QuizReview
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int QuizQandAId { get; set; }
        public QuizQandA QuizQandA { get; set; } = default!;

        public DateTime ReviewedAt { get; set; }
        public ReviewRating Rating { get; set; }

        public double StabilityAfter { get; set; }
        public double DifficultyAfter { get; set; }
        public int IntervalAfter { get; set; }
    }

}
