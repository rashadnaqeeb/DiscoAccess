namespace DiscoAccess.Core.Speech
{
    /// <summary>Monotonic time source, seam so the dedup window is testable with fake time.</summary>
    public interface IClock
    {
        double NowSeconds { get; }
    }
}
