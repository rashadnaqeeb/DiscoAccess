using DiscoAccess.Core.Modularity;
using DiscoAccess.Core.Strings;
using Snv = System.Numerics.Vector3;

namespace DiscoAccess.Module.World
{
    /// <summary>
    /// Names the map district for the two location readouts, modelled on wotr-access's room tracker: the
    /// auto-announce speaks the sub-district the instant the point of attention crosses into a different one
    /// ("Harbour"), and the 'r' key reads the full location, the map name plus the sub-district ("Martinaise,
    /// Harbour"). The reference point is the overlay cursor, which reads the player's position until the cursor
    /// is glided away, so the readout follows the player while walking and the cursor while reviewing.
    ///
    /// The exterior has no runtime notion of sub-districts - the game names whole scenes only - so we author
    /// the partition as labelled anchor points and take the nearest as the district: a Voronoi tiling, so
    /// every walkable point belongs to exactly one district with no overlaps or gaps. Elevated micro-districts
    /// (the Whirling balcony, the Capeside apartment roof and balcony) stack above ground districts on the
    /// same XZ, so height is folded into the distance to keep the layers apart.
    ///
    /// The anchor coordinates are a rough first pass, tuned live by hot-reload during a full playthrough; the
    /// district names are settled and live in <see cref="Strings"/>.
    /// </summary>
    internal sealed class DistrictReader
    {
        // Only Martinaise-ext has authored sub-districts; other scenes read just their map name.
        private const string Scene = "Martinaise-ext";
        // How many metres of horizontal distance one metre of height counts as, so a rooftop micro-district
        // wins over the ground district beneath it only when the reference is actually up there.
        private const float HeightWeight = 2f;

        private readonly struct Anchor
        {
            public readonly string Name;
            public readonly float X, Y, Z;
            public Anchor(string name, float x, float y, float z) { Name = name; X = x; Y = y; Z = z; }
        }

        // Authored anchors for Martinaise-ext (world XZ, plus Y for the elevated micro-districts). Ground
        // anchors sit at Y=1; several districts carry more than one anchor so their concave shapes classify
        // correctly. Coordinates are rough first-pass and get tuned live during the playthrough.
        private static readonly Anchor[] Anchors =
        {
            new Anchor(Strings.DistrictPlaza, -8, 1, -75), new Anchor(Strings.DistrictPlaza, -16, 1, -72),
            new Anchor(Strings.DistrictYard, -20, 1, -118), new Anchor(Strings.DistrictYard, -19, 1, -128),
            new Anchor(Strings.DistrictTrafficJam, -38, 1, -82), new Anchor(Strings.DistrictTrafficJam, -40, 1, -70),
            new Anchor(Strings.DistrictHarbourGate, -52, 1, -100),
            new Anchor(Strings.DistrictHarbour, -58, 1, -128), new Anchor(Strings.DistrictHarbour, -67, 1, -135),
            new Anchor(Strings.DistrictPier, 0, 1, -130), new Anchor(Strings.DistrictPier, 6, 1, -140),
            new Anchor(Strings.DistrictCanal, 10, 1, -40), new Anchor(Strings.DistrictCanal, 0, 1, -52),
            new Anchor(Strings.DistrictFishingVillage, 50, 1, -40), new Anchor(Strings.DistrictFishingVillage, 65, 1, -62),
            new Anchor(Strings.DistrictFishingVillage, 55, 1, -80), new Anchor(Strings.DistrictFishingVillage, 72, 1, -66),
            new Anchor(Strings.DistrictIce, 56, 1, -100),
            new Anchor(Strings.DistrictFishMarket, 83, 1, -127),
            new Anchor(Strings.DistrictLandsEnd, 80, 1, -165), new Anchor(Strings.DistrictLandsEnd, 85, 1, -185),
            new Anchor(Strings.DistrictCoast, 115, 1, -70), new Anchor(Strings.DistrictCoast, 120, 1, -95),
            new Anchor(Strings.DistrictCoast, 110, 1, -56), new Anchor(Strings.DistrictCoast, 128, 1, -88),
            new Anchor(Strings.DistrictSeaFortress, 40, 1, -235), new Anchor(Strings.DistrictSeaFortress, 42, 1, -255),
            new Anchor(Strings.DistrictWhirlingBalcony, -23, 8, -91),
            new Anchor(Strings.DistrictApartmentBalcony, -18, 11, -130),
            new Anchor(Strings.DistrictApartmentRoof, -6, 8, -134),
        };

        private readonly IModHost _host;
        private string _announced; // the sub-district last auto-announced (null off-map / not yet resolved)

        public DistrictReader(IModHost host) { _host = host; }

        /// <summary>Auto-announce: read the sub-district at <paramref name="reference"/> (the overlay cursor,
        /// i.e. cursor-else-player) and speak it the instant it differs from the last. A no-op outside the
        /// authored scene, which resets the tracker so re-entry re-announces.</summary>
        public void Tick(Snv reference, string sceneName)
        {
            if (sceneName != Scene) { _announced = null; return; }

            string district = Nearest(reference);
            if (district == _announced) return;

            _announced = district;
            // Ambient orientation, so it queues behind whatever is speaking rather than interrupting.
            _host.Speech.Speak(district, interrupt: false);
        }

        /// <summary>The 'r' readout: speak the full location, the map name and the sub-district when there is
        /// one. An explicit request, so it interrupts.</summary>
        public void ReadLocation(Snv reference, string sceneName)
        {
            string map = MapName(sceneName);
            string subregion = sceneName == Scene ? Nearest(reference) : null;
            _host.Speech.Speak(Strings.WorldLocation(map, subregion), interrupt: true);
        }

        // The map's spoken name: the game's own localized area name with hyphens read as spaces ("Whirling in
        // Rags"), plus the floor word for a numbered interior level so stacked scenes are distinguishable.
        private static string MapName(string sceneName)
        {
            string localized = I2.Loc.LocalizationManager.GetTranslation("Area Names/" + sceneName);
            string map = (string.IsNullOrEmpty(localized) ? sceneName : localized).Replace('-', ' ');
            string floor = FloorLabel(sceneName);
            return floor == null ? map : map + " " + floor;
        }

        // The floor word from a scene id's level suffix: "-f<n>" reads "floor N", "-s<n>" reads "basement"
        // (a shared basement level). Null when the scene carries no level suffix (the exterior, a flat interior).
        private static string FloorLabel(string sceneName)
        {
            foreach (string tok in sceneName.Split('-'))
            {
                if (tok.Length < 2) continue;
                string digits = tok.Substring(1);
                if (!IsDigits(digits)) continue;
                if (tok[0] == 'f') return Strings.WorldFloor + " " + digits;
                if (tok[0] == 's') return Strings.WorldBasement;
            }
            return null;
        }

        private static bool IsDigits(string s)
        {
            foreach (char c in s) if (c < '0' || c > '9') return false;
            return true;
        }

        // The nearest anchor by height-weighted squared distance; its name is the district.
        private static string Nearest(Snv p)
        {
            string best = null;
            float bd = float.MaxValue;
            foreach (var a in Anchors)
            {
                float dx = p.X - a.X, dy = (p.Y - a.Y) * HeightWeight, dz = p.Z - a.Z;
                float d = dx * dx + dy * dy + dz * dz;
                if (d < bd) { bd = d; best = a.Name; }
            }
            return best;
        }
    }
}
