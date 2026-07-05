using InControl;

namespace DiscoAccess.Module.Input
{
    /// <summary>
    /// Forwards a mod hotkey to the game as a real InControl action press, so the game's own input handlers
    /// open (or refuse) the screen with all their gating - the mod adds none of its own. A control's
    /// <c>WasPressed</c> is read straight off its state, independent of whether the world keyboard has muted
    /// InControl, so the press is written directly onto the action - a pressed <c>thisState</c> over a
    /// released <c>lastState</c> IS <c>WasPressed</c> true - rather than fed through InControl's tick machinery,
    /// which the mute freezes. <see cref="Tick"/> runs from the pump right after input is polled: it writes the
    /// requested press this frame (for the game's handlers to read) and releases the previous one, so each is a
    /// clean one-frame edge.
    /// </summary>
    internal static class GameActionPress
    {
        private static PlayerAction _pending;   // requested this frame, written by the next Tick
        private static PlayerAction _injected;  // written last Tick, released this Tick

        /// <summary>Queue a game action to be pressed; the next <see cref="Tick"/> writes it onto the action.</summary>
        public static void Request(PlayerAction action) => _pending = action;

        /// <summary>Release the previous frame's press, then write this frame's, so the press lasts one frame.</summary>
        public static void Tick()
        {
            if (_injected != null)
            {
                _injected.thisState = new InputControlState();
                _injected = null;
            }
            if (_pending != null)
            {
                var pressed = new InputControlState();
                pressed.Set(true);
                _pending.lastState = new InputControlState();
                _pending.thisState = pressed;
                _injected = _pending;
                _pending = null;
            }
        }
    }
}
