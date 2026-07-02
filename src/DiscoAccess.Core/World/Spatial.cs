using System;
using DiscoAccess.Core.Audio;

namespace DiscoAccess.Core.World
{
    /// <summary>
    /// The pure audio-placement formulas behind the spatial soundscape, factored out of any engine so they
    /// can be unit-tested and tuned in one place. A sensing system computes the geometry (the offset and
    /// distance to the nearest part of a thing) and asks this for the stereo pan and volume, or for the next
    /// sonar sweep gap; the audio engine just plays what it is handed. Ported from the WOTR exploration mod,
    /// with distances in metres (Disco's 1 unit = 1 metre scale).
    /// </summary>
    public static class Spatial
    {
        // Max interaural delay ~ head width / speed of sound ~ 0.22 m / 343 m/s ~ 0.66 ms.
        private const float MaxItdSeconds = 0.00066f;

        // Front/back cue. The wet path is a lowpass closing from open (due-side) to muffled (due-south);
        // the wet MIX rises in step. Because the cue sounds are bright and narrowband, a pure lowpass would
        // silence them behind the listener - so the dry remainder (1 - WetMix) is always kept: broadband
        // sounds darken, bright sounds simply get quieter, and nothing ever disappears.
        private const float OpenHz = 20000f;  // due-side: wet path effectively transparent (and WetMix ~ 0)
        private const float MuffledHz = 500f; // due-south: the wet path is heavily muffled
        private const float MaxWet = 0.5f;    // due-south: 50% filtered / 50% dry (a bright cue ~ -6 dB)

        /// <summary>Stereo pan in [-1, 1] for a thing whose nearest point is <paramref name="dx"/> metres to
        /// the side (east positive) at planar distance <paramref name="dist"/>. Close in, pan tracks the
        /// lateral offset; far out it saturates toward the bearing. <paramref name="panWidth"/> is the
        /// crossover distance. Coincident (dist ~ 0) reads centred.</summary>
        public static float Pan(float dx, float dist, float panWidth)
            => dist > 1e-3f ? WorldMath.Clamp(dx / Math.Max(dist, panWidth), -1f, 1f) : 0f;

        /// <summary>Full direction cues for a thing offset <paramref name="dx"/> metres east and
        /// <paramref name="dz"/> metres north of the listener (the WOTR spatializer model, on the top-down
        /// XZ plane the compass readout uses). East/west becomes the constant-power <see cref="Pan"/> plus
        /// an interaural time difference sharing the pan's lateral fraction, so the time and level cues
        /// move together; north/south becomes timbre - stereo cannot pan front/back, so sources behind the
        /// listener (south) are progressively low-passed, ramping on a log curve from open at the due-side
        /// line to muffled-and-mixed-in at due-south (muffled = behind, bright = ahead, the audiogame
        /// convention). Distance stays the caller's volume job. <paramref name="itd"/> and
        /// <paramref name="frontBack"/> gate the two extra cues (the mod menu toggles) so each can be
        /// A/B'd by ear.</summary>
        public static SpatialCue Cue(float dx, float dz, float panWidth, bool itd = true, bool frontBack = true)
        {
            float dist = (float)Math.Sqrt(dx * dx + dz * dz);
            float lat = Pan(dx, dist, panWidth);
            var cue = new SpatialCue { Pan = lat, LowpassHz = OpenHz };

            if (itd) cue.ItdSeconds = MaxItdSeconds * lat;

            // Only the rear hemisphere is processed (south of the listener exactly), ramping from dry at
            // the due-side line to muffled at due-south.
            if (frontBack && dist > 1e-3f)
            {
                float northFrac = WorldMath.Clamp(dz / dist, -1f, 1f); // +1 ahead .. -1 behind
                if (northFrac < 0f)
                {
                    float back = -northFrac; // 0 at the side line .. 1 at due-south
                    cue.LowpassHz = OpenHz * (float)Math.Pow(MuffledHz / OpenHz, back); // log interp
                    cue.WetMix = back * MaxWet;
                }
            }
            return cue;
        }

        /// <summary>Volume in [<paramref name="floor"/>, 1] falling with distance on the curve
        /// refDist / (refDist + dist): full at the thing, half a reference-distance away, never below the
        /// floor so a far-but-revealed thing stays faintly audible. The per-system and master volumes scale
        /// this on top.</summary>
        public static float DistanceVolume(float dist, float refDist, float floor)
            => WorldMath.Clamp(refDist / (refDist + dist), floor, 1f);

        /// <summary>Wall-tone proximity volume in [0, 1]: 0 at or beyond <paramref name="range"/>, rising
        /// quadratically to 1 right at the wall, so it bites close in and stays quiet at the edge of
        /// range.</summary>
        public static float ProximityVolume(float dist, float range)
        {
            if (dist >= range || range <= 0f) return 0f;
            float t = 1f - dist / range;
            return t * t;
        }

        /// <summary>Seconds between sonar pings for a sweep of <paramref name="count"/> things:
        /// spread / count, clamped to [<paramref name="gapMin"/>, <paramref name="gapMax"/>], so a few feel
        /// spacious and a crowd compresses toward the floor (the whole sweep lengthens, nothing is
        /// dropped).</summary>
        public static float SweepGap(int count, float spread, float gapMin, float gapMax)
            => WorldMath.Clamp(spread / Math.Max(1, count), gapMin, gapMax);
    }
}
