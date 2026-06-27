using System.Text;
using TMPro;
using UnityEngine.UI;

namespace DiscoAccess.Module
{
    /// <summary>
    /// Adapter: pulls the raw text off a focused Selectable by reading the TextMeshPro labels on it
    /// and its children. Extraction only; the Core pipeline cleans the result. DE's UI is uGUI + TMP,
    /// so labels are TMP_Text.
    /// </summary>
    public static class FocusReader
    {
        public static string Read(Selectable selectable)
        {
            var sb = new StringBuilder();
            var labels = selectable.gameObject.GetComponentsInChildren<TMP_Text>(true);
            foreach (var label in labels)
            {
                string t = label.text;
                if (string.IsNullOrEmpty(t))
                    continue;
                if (sb.Length > 0)
                    sb.Append(' ');
                sb.Append(t);
            }
            return sb.ToString();
        }
    }
}
