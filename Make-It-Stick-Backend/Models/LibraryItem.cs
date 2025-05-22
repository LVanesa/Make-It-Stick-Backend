using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Make_It_Stick_Backend.Models
{
    public class LibraryItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LibraryItemId { get; set; }


        [ForeignKey("User")]
        public int UserId { get; set; }
        public AppUser User { get; set; }

        [ForeignKey("Book")]
        public int BookId { get; set; }
        public Book Book { get; set; }


        // Tipul listei (currently_reading, wishlist, finished, abandoned)
        public LibraryShelf LibraryShelf { get; set; }


        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
        public DateTime? DateFinished { get; set; }
        public DateTime? DateStarted { get; set; }
        public int? UserRating { get; set; }


        public int? TotalPages { get; set; } 
        public int CurrentPage { get; set; } = 0;

         
        // Navigare către sesiunile de lectură asociate acestei cărți
        public ICollection<ReadingSession>? ReadingSessions { get; set; }
        public ICollection<BaseNote>? Notes { get; set; }


    }
}
