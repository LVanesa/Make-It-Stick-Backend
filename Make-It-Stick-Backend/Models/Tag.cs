using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Make_It_Stick_Backend.Models
{
    public class Tag
    {
        [Key]

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TagId { get; set; }

        [Required]
        public string Name { get; set; } = default!;

        // Owner of the tag
        public int UserId { get; set; }
        public AppUser User { get; set; } = default!;

        // Many-to-many with notes
        public ICollection<BaseNoteTag> BaseNoteTags { get; set; } = new List<BaseNoteTag>();
    }
}
