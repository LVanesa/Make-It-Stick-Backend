using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Make_It_Stick_Backend.Models
{
    public class AppUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }              // Cheia primară
        public string FirebaseUid { get; set; }  // UID-ul generat de Firebase
        public string Email { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }

        public int? DailyReadingGoal { get; set; }
        public int? YearlyReadingGoal { get; set; }

        public ICollection<LibraryItem>? LibraryItems { get; set; }
    }
}
