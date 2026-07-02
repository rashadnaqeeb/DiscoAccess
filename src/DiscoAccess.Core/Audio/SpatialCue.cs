namespace DiscoAccess.Core.Audio
{
    /// <summary>
    /// The stereo placement for one positional sound, computed by the sensing layer (via
    /// <see cref="DiscoAccess.Core.World.Spatial.Cue"/>) and handed to the engine with the cue to play.
    /// Three perceptual channels, the WOTR exploration mod's model: pan + interaural time difference for
    /// east/west, a rear lowpass + wet mix for north/south; distance stays a caller-owned volume.
    /// Times are seconds, not samples, so Core stays free of the engine's rate.
    /// </summary>
    public struct SpatialCue
    {
        /// <summary>-1 (hard left/west) .. +1 (hard right/east), constant-power.</summary>
        public float Pan;

        /// <summary>Interaural delay in seconds; sign = +east / -west (the far ear is delayed). Below
        /// ~1.5 kHz this is the ear's dominant left/right cue, resolved far finer than the level
        /// difference alone, so it sharpens and externalises the pan - especially on headphones.</summary>
        public float ItdSeconds;

        /// <summary>The wet (rear) path's lowpass cutoff in Hz; lower = more muffled.</summary>
        public float LowpassHz;

        /// <summary>0 = fully dry (ahead/side) rising behind the listener: how much of the filtered
        /// signal replaces the dry one. The dry remainder keeps bright, narrowband cues (which a pure
        /// lowpass would erase) audible - behind then reads as quieter and darker, never silent.</summary>
        public float WetMix;
    }
}
