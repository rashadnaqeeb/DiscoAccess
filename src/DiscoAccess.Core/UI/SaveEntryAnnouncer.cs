using System.Text;
using DiscoAccess.Core.Strings;

namespace DiscoAccess.Core.UI
{
    /// <summary>
    /// Composes the spoken line for a focused save/load list entry from its <see cref="SaveEntryState"/>.
    /// Order follows the house style: the "new save" marker first when this is the create-new slot (the
    /// one fact that changes what activating it does), then the save name (the distinguishing word a
    /// navigator scans for), then its date and time. The name, date, and time are the game's own text
    /// spoken verbatim; only the new-save marker is mod-authored.
    /// </summary>
    public static class SaveEntryAnnouncer
    {
        public static string Compose(SaveEntryState s)
        {
            var sb = new StringBuilder();
            if (s.IsNew) Append(sb, Strings.Strings.StatusNewSave);
            Append(sb, s.Name);
            Append(sb, s.Date);
            Append(sb, s.Time);
            return sb.ToString();
        }

        private static void Append(StringBuilder sb, string? part)
        {
            if (string.IsNullOrEmpty(part))
                return;
            if (sb.Length > 0)
                sb.Append(", ");
            sb.Append(part);
        }
    }
}
