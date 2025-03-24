using System.Text.RegularExpressions;

namespace Make_It_Stick_Backend.Helpers
{
    public static class TextCleaner
    {
        public static string CleanText(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            // Elimină tag-urile HTML
            string output = Regex.Replace(input, "<.*?>", string.Empty);

            // Înlocuiește secvențele de spații, tab-uri și new-line cu un singur spațiu
            output = Regex.Replace(output, @"\s+", " ");

            return output.Trim();
        }
    }
}
