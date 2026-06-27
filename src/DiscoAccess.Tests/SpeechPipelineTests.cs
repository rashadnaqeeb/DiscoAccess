using System.Collections.Generic;
using DiscoAccess.Core.Speech;
using Xunit;

namespace DiscoAccess.Tests
{
    public class SpeechPipelineTests
    {
        private sealed class FakeBackend : ISpeechBackend
        {
            public readonly List<string> Spoken = new List<string>();
            public bool IsAvailable => true;
            public void Speak(string text, bool interrupt) => Spoken.Add(text);
            public void Stop() { }
        }

        private sealed class FakeClock : IClock
        {
            public double NowSeconds { get; set; }
        }

        public SpeechPipelineTests()
        {
            // The tap is a static seam shared across tests; reset it so cases don't leak into each other.
            SpeechPipeline.Spoken = null;
        }

        [Fact]
        public void Speak_InvokesTap_AfterCleanAndDedupGate()
        {
            var pipeline = new SpeechPipeline(new FakeBackend(), new FakeClock());
            var tapped = new List<string>();
            SpeechPipeline.Spoken = (text, interrupt) => tapped.Add(text);

            // Rich-text markup is cleaned before the tap sees it.
            pipeline.Speak("<b>Detective</b>", interrupt: true);

            Assert.Equal(new[] { "Detective" }, tapped);
        }

        [Fact]
        public void Speak_DoesNotInvokeTap_ForDedupedLine()
        {
            var clock = new FakeClock();
            var pipeline = new SpeechPipeline(new FakeBackend(), clock);
            var tapped = new List<string>();
            SpeechPipeline.Spoken = (text, interrupt) => tapped.Add(text);

            pipeline.Speak("anchored cursor");
            pipeline.Speak("anchored cursor"); // within the dedup window: suppressed

            Assert.Equal(new[] { "anchored cursor" }, tapped);
        }
    }
}
