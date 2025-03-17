using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Make_It_Stick_Backend.Models
{
    public class AppUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }              // Cheia primară
        public string FirebaseUid { get; set; }  // UID-ul generat de Firebase
        public string Email { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
    }
}
