using System;
using System.Text;

namespace DiscoAccess.Core.Text
{
    /// <summary>
    /// Restores logical order for Arabic text the game pre-shaped for display. DE's RTL fix (I2's
    /// ArabicFixer, applied to dialogue, actor names, and any term fetched with fixForRTL) converts
    /// logical Arabic into presentation-form glyphs in VISUAL order - reversed, with Latin/digit runs
    /// kept upright and brackets mirrored - so Unity's left-to-right renderer draws it correctly. A
    /// speech synthesizer needs the logical original, so <see cref="Unfix"/> inverts the fix: reverse
    /// the string back, un-mirror the brackets, restore the Latin/digit runs, and fold the
    /// presentation-form glyphs to plain letters (NFKC, which also splits the lam-alef ligatures).
    /// Presentation forms (U+FB50-FDFF, U+FE70-FEFF) never occur in logical text - a keyboard or a
    /// translation file produces base letters - so their presence marks a fixed string and the unfix
    /// is inert on everything else. Tashkeel marks may land before their base letter after the
    /// reversal; game names carry none, and a synthesizer skips stray marks.
    /// </summary>
    public static class RtlText
    {
        public static string Unfix(string s)
        {
            if (string.IsNullOrEmpty(s) || !HasPresentationForms(s))
                return s;

            // The mod composes spoken lines from mixed parts - a fixed game name, our own logical
            // separator and words ("<name>; east, 3 meters"), a fixed speaker and a fixed line
            // ("<who>: <line>") - and only the game-fixed parts need inverting, each within its own
            // position. So unfix per Arabic CLUSTER: a maximal span from one Arabic-script character
            // to the last one reachable without crossing an ASCII ':' or ';' (our composition
            // separators; Arabic text uses its own ؛ ،), interior Latin/digit runs included. A cluster
            // is inverted only when it itself carries presentation forms, so logical Arabic from the
            // mod's own translation table sits untouched next to a fixed game name.
            char[] chars = s.ToCharArray();
            int i = 0;
            while (i < chars.Length)
            {
                if (!IsArabicScript(chars[i])) { i++; continue; }
                int start = i, lastArabic = i;
                bool hasForms = IsPresentationForm(chars[i]);
                for (int j = i + 1; j < chars.Length && chars[j] != ':' && chars[j] != ';'; j++)
                {
                    if (!IsArabicScript(chars[j])) continue;
                    lastArabic = j;
                    hasForms |= IsPresentationForm(chars[j]);
                }
                if (hasForms)
                    UnfixCluster(chars, start, lastArabic);
                i = lastArabic + 1;
            }

            // Presentation-form glyphs to plain letters ("ﻛ" to "ك", lam-alef ligatures to their pair).
            return new string(chars).Normalize(NormalizationForm.FormKC);
        }

        // Invert the fixer over chars[start..end] (inclusive): reverse back to logical order,
        // un-mirror the paired punctuation the fixer mirrored for display, and flip back the Latin
        // and digit runs the fixer kept upright inside the visual text ("27A" to "A72").
        private static void UnfixCluster(char[] chars, int start, int end)
        {
            Array.Reverse(chars, start, end - start + 1);
            for (int i = start; i <= end; i++)
                chars[i] = Mirror(chars[i]);

            int runStart = -1;
            for (int i = start; i <= end + 1; i++)
            {
                bool inRun = i <= end && KeepsLogicalOrder(chars[i]);
                if (inRun && runStart < 0) runStart = i;
                if (!inRun && runStart >= 0)
                {
                    Array.Reverse(chars, runStart, i - runStart);
                    runStart = -1;
                }
            }
        }

        // Any Arabic-script character, base or presentation form: what anchors a cluster.
        private static bool IsArabicScript(char c)
            => (c >= 0x0600 && c <= 0x06FF)     // Arabic
               || (c >= 0x0750 && c <= 0x077F)  // Arabic Supplement
               || IsPresentationForm(c);

        private static bool IsPresentationForm(char c)
            => (c >= 0xFB50 && c <= 0xFDFF)     // Arabic Presentation Forms-A
               || (c >= 0xFE70 && c <= 0xFEFE); // Arabic Presentation Forms-B

        private static bool HasPresentationForms(string s)
        {
            foreach (char c in s)
                if (IsPresentationForm(c))
                    return true;
            return false;
        }

        // A character the fixer leaves in logical order within the visual string: Latin letters
        // (ASCII and Latin-1/Extended) and digits, Western or Eastern Arabic.
        private static bool KeepsLogicalOrder(char c)
        {
            if (c >= '0' && c <= '9') return true;
            if (c >= 'A' && c <= 'Z') return true;
            if (c >= 'a' && c <= 'z') return true;
            if (c >= 0x00C0 && c <= 0x024F && char.IsLetter(c)) return true; // accented Latin
            if (c >= 0x0660 && c <= 0x0669) return true; // Eastern Arabic digits
            if (c >= 0x06F0 && c <= 0x06F9) return true; // Extended (Persian-style) digits
            return false;
        }

        private static char Mirror(char c)
        {
            switch (c)
            {
                case '(': return ')';
                case ')': return '(';
                case '[': return ']';
                case ']': return '[';
                case '{': return '}';
                case '}': return '{';
                case '<': return '>';
                case '>': return '<';
                default: return c;
            }
        }
    }
}
