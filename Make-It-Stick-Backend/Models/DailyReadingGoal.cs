using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Make_It_Stick_Backend.Models
{
    public class DailyReadingGoal
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DailyReadingGoalId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public AppUser User { get; set; }

        public int MinutesPerDay { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    }
}
