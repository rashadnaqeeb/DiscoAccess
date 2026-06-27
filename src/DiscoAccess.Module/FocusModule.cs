using System;
using DiscoAccess.Core.Modularity;
using DiscoAccess.Core.UI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiscoAccess.Module
{
    /// <summary>
    /// The reloadable focus reader, driven each frame by the host pump. Polls DE's NavigationManager for
    /// the current uGUI selection and, on a change, announces it through the host's speech pipeline
    /// (navigation interrupts, per the house rule): an options control reads as label, type, value, and
    /// tooltip via <see cref="OptionAdapter"/>; an Adjust Abilities stat as name, value, grade, and
    /// description via <see cref="AbilityAdapter"/>; a signature skill portrait as name, value, signature
    /// marker, and description via <see cref="SkillAdapter"/>; anything else via the generic
    /// <see cref="FocusReader"/>. It also announces the screen the player just opened, and re-announces
    /// just the changed part when a focused control changes in place (a slider adjusted, a toggle
    /// flipped, an ability raised or lowered, a skill set as signature). This is the implementor
    /// the host loads by interface scan; future dialogue/inventory/world readers and any Harmony patches
    /// join it here.
    /// </summary>
    public sealed class FocusModule : IModModule
    {
        private IModHost _host;
        private Harmony _harmony;
        private IntPtr _lastSelected = IntPtr.Zero;
        // The value-only readout of the focused options control, for detecting an in-place change
        // (adjusting a slider, toggling) where focus does not move. Null when the focus is not an
        // options control.
        private string _lastOptionValue;
        // The value-and-grade readout of the focused ability (Adjust Abilities screen), for detecting an
        // in-place change (pressing plus or minus) where focus does not move. Null when the focus is not
        // an ability control.
        private string _lastAbilityValue;
        // The signature marker of the focused skill (signature skill screen), for detecting the player
        // setting it as their signature where focus does not move. Null when the focus is not a skill.
        private string _lastSkillSignature;
        // The authored name of the screen last announced, for detecting a screen change. Null before any
        // named screen is shown.
        private string _lastScreen;
        // When a screen opens its focus settles over several frames (e.g. options lands on the tab
        // header, then on the first setting), and only the last is worth speaking. While the unscaled
        // clock is below this deadline, focus announcements are skipped so those transients are not voiced
        // and do not interrupt the screen name; the control current when it expires is then spoken once.
        private float _screenSettleUntil = float.NegativeInfinity;
        // Set with the settle window so the control spoken when it expires queues behind the screen name
        // instead of interrupting it; cleared once that one focus is spoken.
        private bool _suppressFocusInterrupt;
        // The message of the confirmation/error popup currently up, for announcing it once on open and
        // again if it changes to a new prompt. Null when no popup is shown.
        private string _lastDialogMessage;

        public void Load(IModHost host)
        {
            _host = host;
            // A per-load id so a reload's Dispose unpatches exactly this load's patches. No patches yet;
            // future readers register them through this instance.
            _harmony = new Harmony("com.rashad.discoaccess.module");

            // Surface any view ScreenAdapter neither names nor silences (e.g. one a game update added),
            // so it is noticed and named rather than going silently unannounced.
            foreach (var view in ScreenAdapter.UnmappedScreens())
                _host.LogWarning($"ScreenAdapter has no name or exclusion for view {view}; it will not be announced.");
        }

        public void Tick()
        {
            // A modal confirmation/error popup (quit, errors, yes/no prompts) runs its own navigation and
            // sets no focus selection, so the focus poller below cannot see it; catch it here and let it
            // own the frame while it is open.
            if (TickDialog())
                return;

            // A newly opened screen is announced here and opens a short settle window; the focus it
            // reveals is then skipped until the window expires and spoken once, queued behind the name.
            TickScreen();

            // Let the screen's focus settle before reading it (see _screenSettleUntil): skip the focus
            // work entirely, leaving _lastSelected unadvanced so the settled control reads when it ends.
            if (Time.unscaledTime < _screenSettleUntil)
                return;

            Selectable selected = CurrentSelectable();
            // Dedup on the native address. (A destroyed Selectable's address can in principle be
            // reused by a new one; in practice a menu rebuild passes through a null-selection frame,
            // which resets this, so that collision effectively can't slip through.)
            IntPtr ptr = selected != null ? selected.Pointer : IntPtr.Zero;

            if (ptr != _lastSelected)
            {
                if (selected == null)
                {
                    _lastSelected = ptr;
                    _lastOptionValue = null;
                    _lastAbilityValue = null;
                    _lastSkillSignature = null;
                    return;
                }

                // A just-opened screen has already interrupted with its name; this first focus queues
                // behind it. Otherwise navigation interrupts as usual.
                bool interrupt = !_suppressFocusInterrupt;
                _suppressFocusInterrupt = false;

                // The in-place re-read trackers below belong to whichever structured control is now
                // focused; clear them up front so a control that is none of these leaves all clear.
                _lastOptionValue = null;
                _lastAbilityValue = null;
                _lastSkillSignature = null;

                OptionState option = OptionAdapter.TryRead(selected, withTooltip: true);
                AbilityState ability = option == null ? AbilityAdapter.TryRead(selected) : null;
                SkillState skill = option == null && ability == null ? SkillAdapter.TryRead(selected) : null;
                if (option != null)
                {
                    _host.Speech.Speak(OptionAnnouncer.Compose(option), interrupt: interrupt);
                    _lastOptionValue = OptionAnnouncer.ComposeValue(option);
                }
                else if (ability != null)
                {
                    // An Adjust Abilities stat reads through its own structured adapter; the generic sweep
                    // would speak the pip diamonds, duplicate the value, and voice the plus/minus arrows.
                    _host.Speech.Speak(AbilityAnnouncer.Compose(ability), interrupt: interrupt);
                    _lastAbilityValue = AbilityAnnouncer.ComposeValue(ability);
                }
                else if (skill != null)
                {
                    // A signature-skill portrait reads through its own structured adapter; the generic
                    // sweep finds only the icon button and speaks nothing, and the on-screen description
                    // panel never follows controller focus.
                    _host.Speech.Speak(SkillAnnouncer.Compose(skill), interrupt: interrupt);
                    _lastSkillSignature = SkillAnnouncer.ComposeSignature(skill);
                }
                else
                {
                    // A save/load entry and an archetype button each read cleanly through their own
                    // structured adapter; the generic sweep would speak a save entry's uppercased name and
                    // "| " timestamp dividers, or an archetype's stacked flip-clock animation layers as
                    // duplicated digits. Other focus falls through to the generic reader.
                    string text = Compose(selected);
                    if (!string.IsNullOrEmpty(text))
                        _host.Speech.Speak(text, interrupt: interrupt);
                }

                // Advance only after a successful read/speak, so an exception in the speech path (caught
                // and logged by the host pump) leaves the change un-acknowledged and retried next frame
                // rather than permanently suppressed.
                _lastSelected = ptr;
                return;
            }

            // Same control still focused: re-announce just the value when it changed in place (a slider
            // adjusted, a toggle flipped, an ability raised or lowered) so the user hears the result of
            // their own input.
            if (selected != null && _lastOptionValue != null)
            {
                OptionState option = OptionAdapter.TryRead(selected, withTooltip: false);
                if (option != null)
                {
                    string value = OptionAnnouncer.ComposeValue(option);
                    if (value != _lastOptionValue)
                    {
                        _host.Speech.Speak(value, interrupt: true);
                        _lastOptionValue = value;
                    }
                }
            }
            else if (selected != null && _lastAbilityValue != null)
            {
                AbilityState ability = AbilityAdapter.TryRead(selected);
                if (ability != null)
                {
                    string value = AbilityAnnouncer.ComposeValue(ability);
                    if (value != _lastAbilityValue)
                    {
                        _host.Speech.Speak(value, interrupt: true);
                        _lastAbilityValue = value;
                    }
                }
            }
            else if (selected != null && _lastSkillSignature != null)
            {
                SkillState skill = SkillAdapter.TryRead(selected);
                if (skill != null)
                {
                    string signature = SkillAnnouncer.ComposeSignature(skill);
                    if (signature != _lastSkillSignature)
                    {
                        // Voice only the gain of the marker (setting this skill as signature); an empty
                        // marker still updates the tracker so it does not re-fire.
                        if (!string.IsNullOrEmpty(signature))
                            _host.Speech.Speak(signature, interrupt: true);
                        _lastSkillSignature = signature;
                    }
                }
            }
        }

        // Route a freshly focused control to its structured adapter, falling back to the generic label
        // sweep. Options are handled by the caller (their value is also polled for in-place changes).
        private static string Compose(Selectable selected)
        {
            SaveEntryState save = SaveEntryAdapter.TryRead(selected);
            if (save != null)
                return SaveEntryAnnouncer.Compose(save);

            ArchetypeState archetype = ArchetypeAdapter.TryRead(selected);
            if (archetype != null)
                return ArchetypeAnnouncer.Compose(archetype);

            return FocusReader.Read(selected);
        }

        // Announce DE's shared confirmation/error popup when it appears, and again if its message changes
        // to a new prompt. Returns whether a popup is up, so Tick skips the focus work it cannot see. The
        // popup supersedes whatever was being said, so it interrupts like navigation. When it closes, the
        // restored focus is forced to re-announce: the popup set no focus, so the dedup would otherwise
        // leave the user not knowing where they landed.
        private bool TickDialog()
        {
            string message = ConfirmDialogAdapter.TryRead();
            if (string.IsNullOrEmpty(message))
            {
                if (_lastDialogMessage != null)
                {
                    _lastSelected = IntPtr.Zero;
                    _lastDialogMessage = null;
                }
                return false;
            }

            if (message != _lastDialogMessage)
            {
                _host.Speech.Speak(message, interrupt: true);
                _lastDialogMessage = message;
            }
            return true;
        }

        // The window after a screen opens during which focus is left to settle before it is read. Long
        // enough to span the open's transient selections (the tab header before the first setting), short
        // enough to stay hidden behind the screen name's own speech, so the settled control adds no
        // perceived delay.
        private const float SettleSeconds = 0.3f;

        // Announce the screen the player just opened, speaking its authored name. The screen name
        // supersedes whatever was being said (the user pressed a button to get here), so it interrupts;
        // the control that lands must then queue behind it rather than clobber it. Opening a settle window
        // both skips the open's transient focuses and holds _suppressFocusInterrupt until the settled
        // control is spoken (the open sets no new selection the dedup would catch on its own, so
        // _lastSelected is reset to force that read).
        private void TickScreen()
        {
            string screen = ScreenAdapter.CurrentScreenName();
            if (screen == _lastScreen)
                return;

            _lastScreen = screen;
            if (string.IsNullOrEmpty(screen))
                return;

            _host.Speech.Speak(screen, interrupt: true);
            _lastSelected = IntPtr.Zero;
            _suppressFocusInterrupt = true;
            _screenSettleUntil = Time.unscaledTime + SettleSeconds;
        }

        // DE drives focus through NavigationManager, but at a freshly opened menu the uGUI EventSystem
        // records the selection a frame or more before NavigationManager does; fall back to it so the
        // initial focus is announced during that window (the dev FocusInspector/InputInjector do the same).
        private static Selectable CurrentSelectable()
        {
            var nav = NavigationManager.Singleton;
            Selectable sel = nav != null ? nav.GetCurrentSelectedSelectable() : null;
            if (sel != null)
                return sel;

            EventSystem es = EventSystem.current;
            GameObject go = es != null ? es.currentSelectedGameObject : null;
            return go != null ? go.GetComponent<Selectable>() : null;
        }

        public void Dispose()
        {
            _harmony?.UnpatchSelf();
            _harmony = null;
            _host = null;
        }
    }
}
