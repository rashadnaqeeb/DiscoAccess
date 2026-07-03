using DiscoAccess.Core.Text;
using Xunit;

namespace DiscoAccess.Tests
{
    /// <summary>
    /// The RTL unfix: display-shaped Arabic (presentation forms in visual order, as DE's fixForRTL
    /// produces for the renderer) comes back to logical order for the synthesizer. The fixed inputs
    /// are real strings captured from the live game running in Arabic.
    /// </summary>
    public class RtlTextTests
    {
        [Fact]
        public void Unfix_RestoresLogicalOrder_ForAFixedActorName()
        {
            // Kim Kitsuragi's localized name as DE's actor lookup returns it: presentation forms,
            // visually ordered ("Kitsuragi Kim" reversed glyph by glyph).
            Assert.Equal("كيم كيتسوراجي", RtlText.Unfix("ﻲﺟارﻮﺴﺘﻴﻛ ﻢﻴﻛ"));
        }

        [Fact]
        public void Unfix_KeepsAnEmbeddedLatinRunUpright()
        {
            // The motor skiff: the fixer leaves "A72" in logical order inside the visual string, so
            // the unfix must flip everything back except that run.
            Assert.Equal("قارب روو A72 بمحرك", RtlText.Unfix("كﺮﺤﻤﺑ A72 وور برﺎﻗ"));
        }

        [Fact]
        public void Unfix_LeavesLogicalArabicAlone()
        {
            // Plain base letters (what a translation file or keyboard produces) carry no presentation
            // forms, so the unfix must not touch them - double-unfixing would garble.
            Assert.Equal("كيم كيتسوراجي", RtlText.Unfix("كيم كيتسوراجي"));
        }

        [Fact]
        public void Unfix_LeavesLatinTextAlone()
        {
            Assert.Equal("Kim Kitsuragi, 3 meters", RtlText.Unfix("Kim Kitsuragi, 3 meters"));
        }

        [Fact]
        public void Unfix_InvertsOnlyTheFixedName_InAComposedScannerLine()
        {
            // The scanner composes a fixed game name with the mod's own logical text; only the name's
            // cluster inverts, in place. (The live bug this pins: a whole-line reversal spoke
            // "above ,meters 3 ,east ;<name>".)
            Assert.Equal("كيم كيتسوراجي; east, 3 meters, above",
                RtlText.Unfix("ﻲﺟارﻮﺴﺘﻴﻛ ﻢﻴﻛ; east, 3 meters, above"));
            Assert.Equal("moving to كيم كيتسوراجي",
                RtlText.Unfix("moving to ﻲﺟارﻮﺴﺘﻴﻛ ﻢﻴﻛ"));
        }

        [Fact]
        public void Unfix_InvertsSpeakerAndLineSeparately_AcrossTheColon()
        {
            // The dialogue reader composes "<speaker>: <line>", both fixed by the game: each cluster
            // inverts within its own position, so the speaker stays first.
            Assert.Equal("كيم: كيم كيتسوراجي", RtlText.Unfix("ﻢﻴﻛ: ﻲﺟارﻮﺴﺘﻴﻛ ﻢﻴﻛ"));
        }

        [Fact]
        public void Unfix_LeavesALogicalCluster_BesideAFixedOne()
        {
            // A translated mod word (logical, from ar.txt) composed after a fixed game name: only the
            // fixed cluster inverts; per-cluster gating keeps the logical one intact.
            Assert.Equal("كيم كيتسوراجي; شرق", RtlText.Unfix("ﻲﺟارﻮﺴﺘﻴﻛ ﻢﻴﻛ; شرق"));
        }

        [Fact]
        public void Clean_UnfixesFixedArabic_EndToEnd()
        {
            // The speech funnel: every reader's text passes TextFilter.Clean, so fixed Arabic from any
            // game surface (dialogue, names, tooltips) reaches the synthesizer logical.
            Assert.Equal("كيم كيتسوراجي", TextFilter.Clean("ﻲﺟارﻮﺴﺘﻴﻛ ﻢﻴﻛ"));
        }

        [Fact]
        public void Clean_StripsBidiControlCharacters()
        {
            // RLM + RLE ... PDF, LRM, and an isolate pair around RTL text: direction marks are visual
            // layout, meaningless to a synthesizer, and some announce them.
            const string rlm = "‏", rle = "‫", pdf = "‬", lrm = "‎";
            const string lri = "⁦", pdi = "⁩";
            string marked = rlm + rle + "مرحبا" + pdf + " " + lrm + "disco" + lri + pdi;
            Assert.Equal("مرحبا disco", TextFilter.Clean(marked));
        }

        [Fact]
        public void TypeAhead_MatchesLogicalTyping_AgainstAFixedLabel()
        {
            // Type-ahead: the user types logical Arabic; a list label read from the game may be
            // display-fixed. Typing "كيم" must land on the fixed "Kim Kitsuragi" label.
            var items = new[] { "Bottle", "ﻲﺟارﻮﺴﺘﻴﻛ ﻢﻴﻛ" };
            var search = new DiscoAccess.Core.UI.Nav.TypeAheadSearch();
            int landed = -1;
            foreach (char c in "كيم") search.AddChar(c);
            search.Search(items.Length, i => items[i], i => landed = i);
            Assert.Equal(1, landed);
        }
    }
}
