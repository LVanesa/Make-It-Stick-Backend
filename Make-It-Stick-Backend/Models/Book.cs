using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Make_It_Stick_Backend.Models
{
    public class Book
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BookId { get; set; }
        public string GoogleBookId { get; set; }
        [Required]
        public string Title { get; set; }
        public string? Authors { get; set; }
        public string? CoverUrl { get; set; }
        public string? ISBN13 { get; set; }
        public string? Description { get; set; }
        public int? PageCount { get; set; }
        public double? AverageRating { get; set; }
        public string? PublishedDate { get; set; }
        public string? Categories { get; set; }
        public string? AboutAuthor { get; set; }

        public string? Genre { get; set; }
    }
}
