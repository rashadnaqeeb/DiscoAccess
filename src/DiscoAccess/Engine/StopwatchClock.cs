using System.Diagnostics;
using DiscoAccess.Core.Speech;

namespace DiscoAccess.Engine
{
    /// <summary>Real IClock: a monotonic stopwatch. (Avoids Unity Time so Core stays engine-free.)</summary>
    public sealed class StopwatchClock : IClock
    {
        private readonly Stopwatch _sw = Stopwatch.StartNew();

        public double NowSeconds => _sw.Elapsed.TotalSeconds;
    }
}
