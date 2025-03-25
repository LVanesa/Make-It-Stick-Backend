using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Make_It_Stick_Backend.Models
{
    public class DailyProgress
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DailyProgressId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public AppUser User { get; set; }

        public DateTime Date { get; set; } // Doar ziua, fără oră

        public int TotalMinutes { get; set; } 
        public bool GoalReached { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; }
    }
}
