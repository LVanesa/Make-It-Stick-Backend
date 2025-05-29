using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Make_It_Stick_Backend.Models
{
    public class QuizQandA
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Leagă această întrebare de o notiță de orice tip
        public int NoteId { get; set; }
        public BaseNote Note { get; set; } = default!;

        // Întrebare + răspuns generate
        public string Question { get; set; } = default!;
        public string Answer { get; set; } = default!;

        // Parametri pentru spaced repetition - FSRS
        public double Stability { get; set; }
        public double Difficulty { get; set; }

        public int Interval { get; set; }
        public DateTime LastReviewDate { get; set; }
        public DateTime NextReviewDate { get; set; }

        public ReviewRating? LastRating { get; set; }
        public bool IsNew { get; set; } = true;

        // Istoric de review (opțional)
        public ICollection<QuizReview> Reviews { get; set; } = new List<QuizReview>();
    }

}
