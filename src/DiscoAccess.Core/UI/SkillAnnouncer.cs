using System.Text;
using static DiscoAccess.Core.Strings.Strings;

namespace DiscoAccess.Core.UI
{
    /// <summary>
    /// Composes the spoken line for a focused skill on the signature skill screen from its
    /// <see cref="SkillState"/>. Order follows the house style: the skill name first (the distinguishing
    /// word, since the player moves across the grid), then its value, then the signature marker when this
    /// skill is the chosen one, then the flavor description last so a quick navigator hears the
    /// mechanical detail before the longer text is cut by the next focus. <see cref="ComposeSignature"/>
    /// yields the signature marker alone (empty when not the signature), for re-announcing the moment the
    /// player sets this focused skill as their signature, where the name was already spoken on focus. The
    /// name and description are the game's own localized text; only the signature marker is mod-authored.
    /// </summary>
    public static class SkillAnnouncer
    {
        public static string Compose(SkillState s)
        {
            return Join(s.Name + " " + s.Value, SignatureWord(s), s.Description);
        }

        public static string ComposeSignature(SkillState s)
        {
            return SignatureWord(s);
        }

        private static string SignatureWord(SkillState s)
        {
            return s.IsSignature ? StatusSignature : string.Empty;
        }

        private static string Join(params string?[] parts)
        {
            var sb = new StringBuilder();
            foreach (string? part in parts)
            {
                if (string.IsNullOrEmpty(part))
                    continue;
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(part);
            }
            return sb.ToString();
        }
    }
}
