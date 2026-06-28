using System.Collections.Generic;
using DiscoAccess.Core.Modularity;
using DiscoAccess.Core.UI.Nav;
using Sunshine.Views;

namespace DiscoAccess.Module.Nav
{
    /// <summary>
    /// Drives our own keyboard UI navigation, the default way menus are read. Each frame it resolves the
    /// active screen from <c>ViewsPagesBridge.Current</c> and, for a screen with a registered
    /// <see cref="Screen"/>, takes the keyboard (mutes the game's menu input), builds that screen's tree
    /// once on entry, attaches the navigator, and speaks the screen name then the landing control. While
    /// the same screen stands it ticks the screen so a rich screen can refresh its dynamic content.
    ///
    /// We OWN the keyboard with one clean, reversible lever: disabling <c>InControl.InputManager</c>, the
    /// upstream input source the game reads ALL its menu input from. Confirmed live that disabling it
    /// kills every menu key at once - directions, submit, AND the Escape/back the per-view
    /// <c>CloseOnEscapeKey</c> path and a NavigationManager toggle both failed to mute. Our own input
    /// polls <c>UnityEngine.Input</c> directly, independent of InControl, so our keys keep working; and
    /// activation calls <c>NavigationManager.Select</c>/<c>Submit</c> directly (not through input), so it
    /// still runs. The lever is reasserted each frame in case the game re-enables InControl (e.g. on
    /// focus/device change).
    ///
    /// The lever is taken ONLY while we actively drive a registered screen: a screen with no registered
    /// <see cref="Screen"/> (not yet migrated) and any frame the caller marks suppressed (a modal popup is
    /// up, running its own navigation) hand the keyboard back to the game, so the legacy focus-follower
    /// fallback and the game's own popups keep working while screens migrate one at a time.
    /// </summary>
    public sealed class ScreenManager
    {
        private readonly IModHost _host;
        private readonly TraditionalNavigator _nav;
        private readonly Dictionary<ViewType, Screen> _screens = new Dictionary<ViewType, Screen>();

        private ViewType _attachedView;
        private bool _haveAttached;
        // Whether we drove (owned the keyboard) last frame, so the lever is restored exactly once when we
        // stop driving rather than forced every frame (which would fight a lock the game itself set).
        private bool _wasOwning;
        // Whether a modal popup suppressed us last frame, so a registered screen re-announces its focus
        // when the popup closes (the popup leaves our navigator's focus untouched but unspoken).
        private bool _wasSuppressed;
        // Whether ViewsPagesBridge.Current has ever returned: before the view system finishes booting it
        // throws, which is expected; once it has worked, a later throw is a real failure.
        private bool _bridgeReady;
        // Whether the "not ready yet" throw has been logged once, so the boot transient surfaces but does
        // not spam every frame until the view system is up.
        private bool _warnedNotReady;

        /// <summary>Whether our navigator is driving a registered screen this frame (lever taken). Set by
        /// <see cref="Tick"/> before input is polled, so the input layer can gate UI keys on it.</summary>
        public bool OwnsKeyboard { get; private set; }

        /// <summary>Whether the view system is up (ViewsPagesBridge.Current read without throwing) this
        /// frame. False during early boot; the focus-follower fallback must skip its own
        /// ViewsPagesBridge/NavigationManager reads until this is true or they throw too.</summary>
        public bool ViewReady { get; private set; }

        public ScreenManager(IModHost host)
        {
            _host = host;
            _nav = new TraditionalNavigator((text, interrupt) => _host.Speech.Speak(text, interrupt));
            Register(new MainMenuScreen());
            Register(new OptionsScreen());
        }

        private void Register(Screen screen) => _screens[screen.ViewType] = screen;

        /// <summary>Route a fired UI action into the navigator. Returns whether it was consumed.</summary>
        public bool Dispatch(string actionKey) => _nav.Handle(actionKey);

        /// <summary>Resolve the active screen and set keyboard ownership for this frame. Call before
        /// polling input. <paramref name="suppressed"/> forces the keyboard back to the game even on a
        /// registered screen (a modal popup is up and owns the frame).</summary>
        public void Tick(bool suppressed)
        {
            bool wasSuppressed = _wasSuppressed;
            _wasSuppressed = suppressed;

            if (!TryGetView(out ViewType view))
            {
                // The view system is not ready yet (early boot): leave the game its input and detach.
                ViewReady = false;
                OwnsKeyboard = false;
                if (_haveAttached) { _nav.Attach(null); _haveAttached = false; }
                return;
            }
            ViewReady = true;

            bool registered = _screens.ContainsKey(view);
            bool own = registered && !suppressed;

            // Take the lever only while we actively drive, reasserted each frame (the game re-enables
            // InControl on focus/device changes), and restore it exactly once when we stop. On frames we
            // never owned, leave the game's own input state alone so we don't fight a lock it set (a
            // cutscene, loading, or unrecognized modal).
            if (own) InControl.InputManager.Enabled = false;
            else if (_wasOwning) InControl.InputManager.Enabled = true;
            _wasOwning = own;
            OwnsKeyboard = own;

            if (!own)
            {
                // Detach only when leaving a registered screen entirely, not for a transient popup over
                // one: keeping the tree and remembered focus lets the screen resume when the popup closes.
                if (!registered && _haveAttached)
                {
                    _nav.Attach(null);
                    _haveAttached = false;
                }
                return;
            }

            Screen screen = _screens[view];
            if (_haveAttached && view == _attachedView)
            {
                // A modal popup just closed over this screen: re-announce where focus is, since the popup
                // left our navigator's focus untouched but the user heard only the popup.
                if (wasSuppressed)
                    _nav.AnnounceCurrent();
                screen.OnUpdate(_host, _nav); // a rich screen refreshes its dynamic content
                return;
            }

            // Build and announce first, then record the attach: if BuildRoot/Speak throws, the flags stay
            // unset so the next frame retries instead of stranding the screen as permanently "attached".
            _nav.Attach(screen.BuildRoot(_host));
            _host.Speech.Speak(screen.ScreenName, interrupt: true); // supersedes; the landing queues behind
            _nav.AnnounceCurrent();
            _attachedView = view;
            _haveAttached = true;
        }

        // Read the current view, treating the early-boot "not ready yet" throw as a transient (silent, the
        // game keeps its input) and a post-ready throw as a real failure worth surfacing.
        private bool TryGetView(out ViewType view)
        {
            try
            {
                view = ViewsPagesBridge.Current;
                _bridgeReady = true;
                return true;
            }
            catch (System.Exception e)
            {
                view = default;
                // Expected only during early boot. Surface the first occurrence (so a view system that
                // never comes up is visible) and any failure once it has worked, but stay silent for the
                // rest of the boot transient so it does not spam every frame.
                if (_bridgeReady || !_warnedNotReady)
                {
                    _warnedNotReady = true;
                    _host.LogWarning("ScreenManager: ViewsPagesBridge.Current not ready: " + e.Message);
                }
                return false;
            }
        }

        /// <summary>Hand the keyboard back to the game and detach, for module teardown so a reload never
        /// leaves InControl disabled.</summary>
        public void HandBack()
        {
            InControl.InputManager.Enabled = true;
            _nav.Attach(null);
            _haveAttached = false;
            _wasOwning = false;
            OwnsKeyboard = false;
        }
    }
}
