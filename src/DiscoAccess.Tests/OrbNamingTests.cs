using DiscoAccess.Core.World;
using Xunit;

namespace DiscoAccess.Tests
{
    public class OrbNamingTests
    {
        [Fact]
        public void TextOverride_Wins()
        {
            Assert.Equal("Most of Mullen",
                OrbNaming.Resolve("Most of Mullen", "teaser", "PLAZA ORB / crack"));
        }

        [Fact]
        public void Morsel_UsedWhenNoOverride()
        {
            Assert.Equal("A faint scratching, just out of reach",
                OrbNaming.Resolve(null, "A faint scratching, just out of reach", "PLAZA ORB / crack"));
        }

        [Fact]
        public void Slug_ClueLeadsWithOrbWord()
        {
            Assert.Equal("crack orb", OrbNaming.Resolve(null, null, "PLAZA ORB / crack"));
        }

        [Fact]
        public void Slug_MultiWordClueSurvives()
        {
            Assert.Equal("halogen watermarks orb",
                OrbNaming.Resolve("", "", "KINEEMA ORB / halogen watermarks"));
        }

        [Fact]
        public void Slug_AreaPrefixDropped()
        {
            Assert.Equal("hum aid macaronis orb",
                OrbNaming.Resolve(null, null, "JAM ORB / hum aid macaronis"));
        }

        [Fact]
        public void TitleWithoutScaffolding_TakesWholeClue()
        {
            Assert.Equal("crack orb", OrbNaming.Resolve(null, null, "crack"));
        }

        [Fact]
        public void NoClue_FallsBackToBareOrbWord()
        {
            Assert.Equal("orb", OrbNaming.Resolve(null, null, "  "));
            Assert.Equal("orb", OrbNaming.Resolve(null, null, "PLAZA ORB / "));
        }
    }
}
