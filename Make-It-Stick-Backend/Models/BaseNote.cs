using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Make_It_Stick_Backend.Models
{
    public abstract class BaseNote
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NoteId { get; set; }
        public int UserId { get; set; } 
        public AppUser User { get; set; } = default!;
        public int LibraryItemId { get; set; }
        public LibraryItem LibraryItem { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsFavorite { get; set; } = false;
        public ICollection<BaseNoteTag> BaseNoteTags { get; set; } = new List<BaseNoteTag>();
    }

}
