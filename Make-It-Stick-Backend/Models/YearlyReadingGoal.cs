namespace Make_It_Stick_Backend.Models
{
    public class YearlyReadingGoal
    {
        public int Id { get; set; }

        // Cheie străină către utilizator
        public int UserId { get; set; }
        public AppUser User { get; set; }

        // Anul obiectivului (ex: 2025)
        public int Year { get; set; }

        // Numărul de cărți propuse pentru acel an
        public int TargetBooks { get; set; }

        // Progresul curent
        public int BooksRead { get; set; }

        // Dată creare/modificare
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Colecția cărților citite în acel an (opțional)
        public ICollection<Book>? Books { get; set; }
    }

}
