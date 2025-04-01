public class ReadingSessionDto
{
    public int LibraryItemId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int StartPage { get; set; }
    public int EndPage { get; set; }
    public int? TotalPages { get; set; }  // <-- Asta e nou
}
