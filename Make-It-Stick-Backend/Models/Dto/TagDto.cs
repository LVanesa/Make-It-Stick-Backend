namespace Make_It_Stick_Backend.Models.Dto
{
    public class TagDto
    {
        public int TagId { get; set; } // optional în POST, util în GET
        public string Name { get; set; } = string.Empty;
    }

}
