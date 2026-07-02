namespace DiscoAccess.Core.Audio
{
    /// <summary>
    /// A named one-shot cue the sensing layer can fire (the engine owns the sound file behind each name, so
    /// Core stays free of paths). Today: the cursor's enter/exit blips as it glides across a thing's
    /// footprint - a rising click on entering, a falling click on leaving to bare ground - and the
    /// impassable bump when a glide is refused at the edge of the senses.
    /// </summary>
    public enum AudioCue
    {
        /// <summary>The cursor entered a thing's footprint (rising click).</summary>
        CursorEnter,

        /// <summary>The cursor left a thing to bare ground (falling click).</summary>
        CursorExit,

        /// <summary>A glide was refused at the edge of the senses - the visible frame's border or unrevealed
        /// fog-of-war ground. Distinct from the wall tones: a wall means "no path", this means "walk here
        /// with your body and there will be more".</summary>
        CursorImpassable,
    }
}
