using System.Collections.Generic;
using DiscoAccess.Core.UI;
using DiscoAccess.Core.UI.Nav;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiscoAccess.Module.Nav
{
    /// <summary>
    /// A navigable options control (slider, toggle, or dropdown), wrapping the live
    /// <see cref="OptionSelectableController"/>. It reads its state live via <see cref="OptionAdapter"/>
    /// and composes the spoken line with the Core <see cref="OptionAnnouncer"/>, so the rich screen
    /// speaks exactly what the focus-follower did (name, type, value, and on focus the tooltip).
    ///
    /// Driving changes the underlying uGUI component directly - <c>Slider.value</c>, <c>Toggle.isOn</c>,
    /// <c>TMP_Dropdown.value</c> - whose <c>onValueChanged</c> is the game's own apply path. That is the
    /// only way to change a setting while our keyboard lever mutes InControl: a toggle activates (Enter),
    /// a slider and a dropdown step on Left/Right (a dropdown cycles its options).
    /// </summary>
    public sealed class OptionControl : UIElement
    {
        private readonly OptionSelectableController _osc;

        // A continuous slider steps a twentieth of its travel per press (a 0..1 volume slider moves 5%).
        private const float ContinuousStep = 0.05f;

        public OptionControl(OptionSelectableController osc) => _osc = osc;

        private Selectable Sel => _osc.selectable;

        // Focusable only while shown and interactable; a setting greyed out (a dependent option) drops out.
        public override bool CanFocus => Sel != null && Sel.isActiveAndEnabled && Sel.interactable;

        // The setting name alone, for the navigator's container-label dedup (the full readout is in
        // GetFocusText). Read live, never cached.
        public override string Label
        {
            get { OptionState s = Read(withTooltip: false); return s?.Label; }
        }

        // Just the value, for re-announcing an in-place change (a slider stepped, a toggle flipped); the
        // navigator speaks this after an activate/increase/decrease.
        public override string Value
        {
            get { OptionState s = Read(withTooltip: false); return s != null ? OptionAnnouncer.ComposeValue(s) : null; }
        }

        // Activating (a toggle) flips the value in place, so the navigator re-announces it. Sliders and
        // dropdowns re-announce through the Left/Right (increase/decrease) path instead.
        public override bool ReannounceOnActivate => true;

        // The full focus readout: name, type, value, tooltip - the same composition the focus-follower
        // used, via the unit-tested Core composer.
        public override string GetFocusText()
        {
            OptionState s = Read(withTooltip: true);
            return s != null ? OptionAnnouncer.Compose(s) : string.Empty;
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            OptionState s = Read(withTooltip: false);
            if (s == null) yield break;
            switch (s.Kind)
            {
                case OptionControlKind.Toggle:
                    yield return new ElementAction(ActionIds.Activate, ToggleValue);
                    break;
                case OptionControlKind.Slider:
                    yield return new ElementAction(ActionIds.Decrease, () => StepSlider(-1));
                    yield return new ElementAction(ActionIds.Increase, () => StepSlider(1));
                    break;
                case OptionControlKind.Dropdown:
                    yield return new ElementAction(ActionIds.Decrease, () => StepDropdown(-1));
                    yield return new ElementAction(ActionIds.Increase, () => StepDropdown(1));
                    break;
            }
        }

        // Move the game's cursor to this control as our focus lands, so its selection follows ours.
        public override void OnFocused() => GameCursor.Follow(Sel);

        private OptionState Read(bool withTooltip)
        {
            Selectable sel = Sel;
            return sel != null ? OptionAdapter.TryRead(sel, withTooltip) : null;
        }

        // The casts below are guaranteed by the Kind the action was registered for (GetActions reads it
        // from the same Sel), so they are not null-guarded; a mismatch would surface as a logged crash in
        // the pump rather than a silently dropped change.
        private void ToggleValue()
        {
            Toggle t = Sel.TryCast<Toggle>();
            t.isOn = !t.isOn;
        }

        private void StepSlider(int dir)
        {
            Slider sl = Sel.TryCast<Slider>();
            float step = sl.wholeNumbers ? 1f : ContinuousStep * (sl.maxValue - sl.minValue);
            sl.value = Mathf.Clamp(sl.value + dir * step, sl.minValue, sl.maxValue);
        }

        private void StepDropdown(int dir)
        {
            TMP_Dropdown dd = Sel.TryCast<TMP_Dropdown>();
            dd.value = Mathf.Clamp(dd.value + dir, 0, dd.options.Count - 1);
        }
    }
}
