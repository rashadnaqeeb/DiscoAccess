using TMPro;
using UnityEngine.UI;
using DiscoAccess.Core.UI.Nav;

namespace DiscoAccess.Module.Nav
{
    /// <summary>
    /// A read-only entry on the options Controls tab: an action and the key bound to it (e.g. "Movement,
    /// left click"). The action text is read live from its label; the key is translated from the paired
    /// glyph's icon sprite via <see cref="KeyGlyph"/>, since DE draws the key as an image, not text. Read
    /// live, never cached. A control with no resolvable glyph reads just its action.
    /// </summary>
    public sealed class ControlReference : UIElement
    {
        private readonly TMP_Text _label;
        private readonly Image _glyph;

        public ControlReference(TMP_Text label, Image glyph)
        {
            _label = label;
            _glyph = glyph;
        }

        public override bool CanFocus => _label != null && _label.isActiveAndEnabled;

        public override string Label => _label != null ? GameLocalization.Cased(_label) : null;

        public override string Value => _glyph != null && _glyph.sprite != null ? KeyGlyph.Read(_glyph.sprite.name) : null;
    }
}
