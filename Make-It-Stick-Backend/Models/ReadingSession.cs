using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Make_It_Stick_Backend.Models
{
    public class ReadingSession
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReadingSessionId { get; set; }

        [ForeignKey("LibraryItem")]
        public int LibraryItemId { get; set; }
        public LibraryItem LibraryItem { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public int StartPage { get; set; }
        public int EndPage { get; set; }

        
        public int? Duration { get; set; }     // în minute/secunde
        public int? PagesRead { get; set; }    // EndPage - StartPage

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
