using System.Text.RegularExpressions;

namespace DiscoAccess.Core.Text
{
    /// <summary>
    /// Normalizes raw game text for speech. DE labels are TextMeshPro, so they carry rich-text
    /// markup (&lt;color&gt;, &lt;b&gt;, &lt;sprite&gt;, ...) and hard line breaks that a screen reader
    /// should not voice. Strip the markup, collapse whitespace. Pure and unit-tested.
    /// </summary>
    public static class TextFilter
    {
        private static readonly Regex RichTags = new Regex("<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex Whitespace = new Regex("\\s+", RegexOptions.Compiled);

        public static string Clean(string? raw)
        {
            if (string.IsNullOrEmpty(raw))
                return string.Empty;

            string s = RichTags.Replace(raw, string.Empty);
            s = s.Replace(' ', ' ');   // non-breaking space
            s = s.Replace('​', ' ');   // zero-width space TMP sometimes injects
            s = Whitespace.Replace(s, " ").Trim();
            return s;
        }
    }
}
