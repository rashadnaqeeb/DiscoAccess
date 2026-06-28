using DiscoAccess.Core.Strings;

namespace DiscoAccess.Module.Nav
{
    /// <summary>
    /// Translates DE's keyboard control-glyph sprite names into readable key names. The options Controls
    /// tab draws each key as an icon image, not text, so the bound key lives only in the glyph's sprite
    /// name ("F1icon", "ESCicon", "Cicon"). Letters and function keys are the sprite name minus the "icon"
    /// suffix and read as written; the rest map to authored words. An unrecognized glyph reads its raw
    /// sprite name so it is at least audible rather than dropped.
    /// </summary>
    public static class KeyGlyph
    {
        public static string Read(string sprite)
        {
            if (string.IsNullOrEmpty(sprite))
                return null;
            switch (sprite)
            {
                case "hardware": return null; // the decorative keyboard image, not a key
                case "ESCicon": return Strings.KeyEscape;
                case "TABicon": return Strings.KeyTab;
                case "CLICK-LEFT-icon": return Strings.KeyLeftClick;
                case "CLICK-RIGHT-icon": return Strings.KeyRightClick;
                case "MOUSEwheel": return Strings.KeyMouseWheel;
            }
            // "<key>icon" -> the key itself (single letters and function keys are already readable).
            if (sprite.EndsWith("icon"))
                return sprite.Substring(0, sprite.Length - 4);
            return sprite;
        }
    }
}
