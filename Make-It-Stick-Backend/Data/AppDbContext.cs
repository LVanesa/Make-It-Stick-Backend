using Make_It_Stick_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Make_It_Stick_Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
        {
        }
        public DbSet<Book> Books { get; set; }
        public DbSet<AppUser> Users { get; set; }
        public DbSet<LibraryItem> LibraryItems { get; set; }
        public DbSet<YearlyReadingGoal> YearlyReadingGoals { get; set; }
        public DbSet<ReadingSession> ReadingSessions { get; set; }
        public DbSet<DailyReadingGoal> DailyReadingGoals { get; set; }
        public DbSet<DailyProgress> DailyProgresses { get; set; }
        public DbSet<BaseNote> Notes { get; set; } = default!;
        public DbSet<SimpleNote> SimpleNotes { get; set; } = default!;
        public DbSet<QuoteNote> QuoteNotes { get; set; } = default!;
        public DbSet<DefinitionNote> DefinitionNotes { get; set; } = default!;
        public DbSet<FlashcardNote> FlashcardNotes { get; set; } = default!;
        public DbSet<Tag> Tags { get; set; } = default!;
        public DbSet<BaseNoteTag> BaseNoteTags { get; set; } = default!;
        public DbSet<QuizQandA> QuizQuestions { get; set; } = default!;
        public DbSet<QuizReview> QuizReviews { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configurări suplimentare, dacă este necesar
            base.OnModelCreating(modelBuilder);

            // Relația între AppUser și LibraryItem (un utilizator are multe LibraryItem-uri)
            modelBuilder.Entity<LibraryItem>()
                .HasOne(li => li.User)
                .WithMany(u => u.LibraryItems)
                .HasForeignKey(li => li.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relația între Book și LibraryItem (o carte poate fi în multe LibraryItem-uri)
            modelBuilder.Entity<LibraryItem>()
                .HasOne(li => li.Book)
                .WithMany()
                .HasForeignKey(li => li.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relația între AppUser și YearlyReadingGoal
            modelBuilder.Entity<YearlyReadingGoal>()
                .HasOne(y => y.User)
                .WithMany(u => u.YearlyReadingGoals)
                .HasForeignKey(y => y.UserId);

            // Relația între LibraryItem și ReadingSession
            modelBuilder.Entity<ReadingSession>()
                .HasOne(rs => rs.LibraryItem)
                .WithMany(li => li.ReadingSessions)
                .HasForeignKey(rs => rs.LibraryItemId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relația între AppUser și DailyReadingGoal
            modelBuilder.Entity<AppUser>()
                 .HasOne(u => u.DailyReadingGoal)
                 .WithOne(g => g.User)
                 .HasForeignKey<DailyReadingGoal>(g => g.UserId)
                 .OnDelete(DeleteBehavior.Cascade);


            // Relația între AppUser și DailyProgress
            modelBuilder.Entity<DailyProgress>()
                .HasOne(dp => dp.User)
                .WithMany(u => u.DailyProgresses)
                .HasForeignKey(dp => dp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Constrângere unică pe (UserId, Date) în DailyProgress
            // (un utilizator poate avea un singur DailyProgress pentru fiecare zi)
            modelBuilder.Entity<DailyProgress>()
                .HasIndex(dp => new { dp.UserId, dp.Date })
                .IsUnique();

            modelBuilder.Entity<BaseNote>()
                .HasDiscriminator<string>("NoteType")
                .HasValue<SimpleNote>("Simple")
                .HasValue<QuoteNote>("Quote")
                .HasValue<DefinitionNote>("Definition")
                .HasValue<FlashcardNote>("Flashcard");

            modelBuilder.Entity<BaseNote>()
                .HasOne(n => n.LibraryItem)
                .WithMany(li => li.Notes)
                .HasForeignKey(n => n.LibraryItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AppUser>()
                .HasMany(u => u.Notes)
                .WithOne(n => n.User)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BaseNoteTag>()
                .HasKey(nt => new { nt.NoteId, nt.TagId });

            modelBuilder.Entity<BaseNoteTag>()
                .HasOne(nt => nt.Note)
                .WithMany(n => n.BaseNoteTags)
                .HasForeignKey(nt => nt.NoteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BaseNoteTag>()
                .HasOne(nt => nt.Tag)
                .WithMany(t => t.BaseNoteTags)
                .HasForeignKey(nt => nt.TagId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Tag>()
                .HasOne(t => t.User)
                .WithMany(u => u.Tags) 
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // QuizQandA → BaseNote
            modelBuilder.Entity<QuizQandA>()
                .HasOne(q => q.Note)
                .WithMany()
                .HasForeignKey(q => q.NoteId)
                .OnDelete(DeleteBehavior.Cascade);

            // QuizReview → QuizQandA
            modelBuilder.Entity<QuizReview>()
                .HasOne(r => r.QuizQandA)
                .WithMany(q => q.Reviews)
                .HasForeignKey(r => r.QuizQandAId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index pentru interogare eficientă a cardurilor zilnice
            modelBuilder.Entity<QuizQandA>()
                .HasIndex(q => q.NextReviewDate);

            // Enum mapping (opțional)
            modelBuilder.Entity<QuizReview>()
                .Property(r => r.Rating)
                .HasConversion<int>();

            modelBuilder.Entity<QuizQandA>()
                .Property(q => q.LastRating)
                .HasConversion<int>();

        }

    }
}
