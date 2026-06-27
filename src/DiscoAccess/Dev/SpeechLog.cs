using System.Collections.Generic;
using System.Text;

namespace DiscoAccess.Dev
{
    /// <summary>
    /// Thread-safe ring buffer of strings the mod has spoken, so the dev driver (which can't hear TTS)
    /// can read back what was said. Each line has a stable monotonic index; callers poll with a cursor
    /// to get only what's new.
    /// </summary>
    internal sealed class SpeechLog
    {
        private const int Capacity = 500;

        private readonly object _lock = new object();
        private readonly List<string> _lines = new List<string>();
        private long _base; // global index of _lines[0]

        public void Add(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;
            lock (_lock)
            {
                _lines.Add(text);
                if (_lines.Count > Capacity)
                {
                    _lines.RemoveAt(0);
                    _base++;
                }
            }
        }

        /// <summary>
        /// Render lines with global index &gt;= <paramref name="since"/>, one per line as "index: text".
        /// <paramref name="next"/> returns the cursor to pass next time to get only newer lines.
        /// </summary>
        public string Render(long since, out long next)
        {
            lock (_lock)
            {
                long end = _base + _lines.Count; // exclusive
                if (since < _base)
                    since = _base;
                var sb = new StringBuilder();
                for (long i = since; i < end; i++)
                    sb.Append(i).Append(": ").Append(_lines[(int)(i - _base)]).Append('\n');
                next = end;
                return sb.ToString();
            }
        }
    }
}
