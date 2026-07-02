using System;
using DiscoAccess.Core.World;
using Xunit;

namespace DiscoAccess.Tests
{
    public class SpatialTests
    {
        [Fact]
        public void Pan_Coincident_IsCentred()
        {
            Assert.Equal(0f, Spatial.Pan(0f, 0f, 3f), 3);
        }

        [Fact]
        public void Pan_HardLeftAndRight_Saturate()
        {
            // Lateral offset well past the pan-width crossover at its own distance saturates to the edge.
            Assert.Equal(1f, Spatial.Pan(10f, 10f, 3f), 3);
            Assert.Equal(-1f, Spatial.Pan(-10f, 10f, 3f), 3);
        }

        [Fact]
        public void DistanceVolume_FullAtSource_HalfAtRefDistance_FlooredFar()
        {
            Assert.Equal(1f, Spatial.DistanceVolume(0f, 3f, 0.08f), 3);
            Assert.Equal(0.5f, Spatial.DistanceVolume(3f, 3f, 0.08f), 3); // refDist/(refDist+dist) = 3/6
            Assert.Equal(0.08f, Spatial.DistanceVolume(1000f, 3f, 0.08f), 3); // clamped to the floor
        }

        [Fact]
        public void ProximityVolume_ZeroAtRange_OneAtWall_Quadratic()
        {
            Assert.Equal(0f, Spatial.ProximityVolume(3f, 3f), 3);   // at range
            Assert.Equal(0f, Spatial.ProximityVolume(5f, 3f), 3);   // beyond range
            Assert.Equal(1f, Spatial.ProximityVolume(0f, 3f), 3);   // at the wall
            Assert.Equal(0.25f, Spatial.ProximityVolume(1.5f, 3f), 3); // halfway -> (0.5)^2
        }

        [Fact]
        public void SweepGap_SpaciousForFew_CompressesForCrowd()
        {
            // spread/count clamped to [min,max]. One thing hits the max; a crowd hits the min.
            Assert.Equal(0.2f, Spatial.SweepGap(1, 0.75f, 0.1f, 0.2f), 3);
            Assert.Equal(0.1f, Spatial.SweepGap(20, 0.75f, 0.1f, 0.2f), 3);
            Assert.Equal(0.15f, Spatial.SweepGap(5, 0.75f, 0.1f, 0.2f), 3); // 0.75/5 = 0.15, in range
        }

        [Fact]
        public void Cue_DueEast_PansRight_LeadsRightEar_StaysBright()
        {
            var cue = Spatial.Cue(10f, 0f, 3f);
            Assert.Equal(1f, cue.Pan, 3);
            Assert.Equal(0.00066f, cue.ItdSeconds, 5); // max head delay, left (far) ear lagging
            Assert.Equal(20000f, cue.LowpassHz, 0);    // at the side line: no rear filter
            Assert.Equal(0f, cue.WetMix, 3);
        }

        [Fact]
        public void Cue_DueWest_MirrorsTheSigns()
        {
            var cue = Spatial.Cue(-10f, 0f, 3f);
            Assert.Equal(-1f, cue.Pan, 3);
            Assert.Equal(-0.00066f, cue.ItdSeconds, 5);
        }

        [Fact]
        public void Cue_Ahead_IsCentredAndDry()
        {
            var cue = Spatial.Cue(0f, 5f, 3f);
            Assert.Equal(0f, cue.Pan, 3);
            Assert.Equal(0f, cue.ItdSeconds, 5);
            Assert.Equal(20000f, cue.LowpassHz, 0);
            Assert.Equal(0f, cue.WetMix, 3);
        }

        [Fact]
        public void Cue_DueSouth_FullyMuffledAndMixed()
        {
            var cue = Spatial.Cue(0f, -5f, 3f);
            Assert.Equal(0f, cue.Pan, 3);
            Assert.Equal(500f, cue.LowpassHz, 0); // the muffled floor
            Assert.Equal(0.5f, cue.WetMix, 3);    // half filtered, half dry
        }

        [Fact]
        public void Cue_HalfwayBehind_RampsCutoffLogarithmically()
        {
            // dz/dist = -0.5 (30 degrees below the side line): cutoff at the log midpoint of the half-ramp,
            // 20000 * (500/20000)^0.5 ~ 3162 Hz, and the wet mix at a quarter.
            float dx = (float)Math.Sqrt(75.0), dz = -5f; // dist = 10
            var cue = Spatial.Cue(dx, dz, 3f);
            Assert.Equal(3162.3f, cue.LowpassHz, 1);
            Assert.Equal(0.25f, cue.WetMix, 3);
        }

        [Fact]
        public void Cue_Toggles_DropEachExtraCueAlone()
        {
            var noItd = Spatial.Cue(10f, 0f, 3f, itd: false);
            Assert.Equal(1f, noItd.Pan, 3);           // pan survives
            Assert.Equal(0f, noItd.ItdSeconds, 5);    // delay gated off

            var noFilter = Spatial.Cue(0f, -5f, 3f, frontBack: false);
            Assert.Equal(20000f, noFilter.LowpassHz, 0); // behind, but the filter is gated off
            Assert.Equal(0f, noFilter.WetMix, 3);
        }
    }
}
