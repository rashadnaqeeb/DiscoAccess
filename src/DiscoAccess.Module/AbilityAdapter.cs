using DiscoAccess.Core.UI;
using TMPro;
using UnityEngine.UI;

namespace DiscoAccess.Module
{
    /// <summary>
    /// Adapter: turns a focused ability on the Adjust Abilities (Create Your Own) screen into a
    /// Unity-free <see cref="AbilityState"/> for Core to compose. An ability control is a
    /// <see cref="StatPanel"/>; its settled score is the target of <c>abilityGradeFlipClock</c> (read
    /// past the split-flap animation, like the archetype reader reads its flip clocks), which doubles as
    /// the index into <c>AbilityGradeFlipClock.GradeStrings</c> for the qualitative grade word. The name
    /// is the ability's localized full name (Intellect, not the on-screen INT); the description is the
    /// sibling <c>AbilityDescriptionText</c> of the grade clock's label panel. A StatPanel shown without
    /// a grade clock (the character sheet reuses the type) has no settled value to read here, so it
    /// returns null and falls through to the generic reader. Extraction only; no caching past the live
    /// read.
    /// </summary>
    public static class AbilityAdapter
    {
        public static AbilityState TryRead(Selectable selectable)
        {
            var panel = selectable.GetComponent<StatPanel>();
            if (panel == null || panel.abilityGradeFlipClock == null)
                return null;

            int value = panel.abilityGradeFlipClock.targetValue;
            string name = GameLocalization.Translate("Abilities/ABILITY_NAME_" + panel.ability);
            return new AbilityState(name, value, Grade(value), Description(panel));
        }

        // The grade word for a score, from the game's grade table indexed by the score itself. Index 0 is
        // an empty placeholder (no ability scores zero), so an out-of-range or empty entry yields no grade.
        private static string Grade(int value)
        {
            var grades = AbilityGradeFlipClock.GradeStrings;
            if (grades == null || value < 0 || value >= grades.Length)
                return null;
            string word = grades[value];
            return string.IsNullOrEmpty(word) ? null : TitleCase(word);
        }

        // The one-line ability description sits beside the grade clock in its label panel, not under the
        // focused control, so the generic child sweep never reaches it; read it directly here.
        private static string Description(StatPanel panel)
        {
            var label = panel.abilityGradeFlipClock.transform.parent.Find("AbilityDescriptionText");
            var text = label != null ? label.GetComponent<TMP_Text>() : null;
            return text != null ? text.text : null;
        }

        // DE stores grade words ALL-CAPS for display ("GOOD"), which reads oddly; recase to natural case
        // for speech. The grades are single words, but capitalize each word to stay safe across languages.
        private static string TitleCase(string text)
        {
            char[] chars = text.ToLowerInvariant().ToCharArray();
            bool wordStart = true;
            for (int i = 0; i < chars.Length; i++)
            {
                if (char.IsWhiteSpace(chars[i]))
                {
                    wordStart = true;
                    continue;
                }
                if (wordStart)
                    chars[i] = char.ToUpperInvariant(chars[i]);
                wordStart = false;
            }
            return new string(chars);
        }
    }
}
