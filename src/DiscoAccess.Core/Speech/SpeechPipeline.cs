using System;
using DiscoAccess.Core.Text;

namespace DiscoAccess.Core.Speech
{
    /// <summary>
    /// The single funnel for everything the mod says. Owns policy: clean the text (strip TMP markup),
    /// drop immediate duplicates within a short window, and route to the backend. Callers never touch
    /// the backend directly. House rule (from the reference mods): navigation interrupts, ambient
    /// announcements queue.
    /// </summary>
    public sealed class SpeechPipeline
    {
        /// <summary>Set once by the plugin at load; null in unit tests that construct their own.</summary>
        public static SpeechPipeline? Instance { get; set; }

        /// <summary>
        /// Optional tap invoked with (text, interrupt) for every line that clears the clean/dedup gate,
        /// so it sees exactly what was voiced. The dev server sets this to read spoken text back (it
        /// can't hear the TTS). Null in normal play and in unit tests.
        /// </summary>
        public static Action<string, bool>? Spoken;

        private const double DedupWindowSeconds = 0.5;

        private readonly ISpeechBackend _backend;
        private readonly IClock _clock;

        private string _lastText = string.Empty;
        private double _lastTime = double.NegativeInfinity;

        public bool Enabled { get; set; } = true;

        public SpeechPipeline(ISpeechBackend backend, IClock clock)
        {
            _backend = backend;
            _clock = clock;
        }

        public void Speak(string? text, bool interrupt = false)
        {
            if (!Enabled)
                return;

            string clean = TextFilter.Clean(text);
            if (clean.Length == 0)
                return;

            double now = _clock.NowSeconds;
            if (clean == _lastText && now - _lastTime < DedupWindowSeconds)
                return;

            _lastText = clean;
            _lastTime = now;
            _backend.Speak(clean, interrupt);
            Spoken?.Invoke(clean, interrupt);
        }

        public void Stop()
        {
            _backend.Stop();
            // Let the same line be spoken again after an explicit stop.
            _lastText = string.Empty;
        }
    }
}
