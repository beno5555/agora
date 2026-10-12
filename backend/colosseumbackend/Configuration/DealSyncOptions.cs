namespace colosseumbackend.Configuration;

public class DealSyncOptions
{
    public const string SectionName = "DealSync";

    public TimeSpan Interval  { get; set; } = TimeSpan.FromMinutes(1);
    /// <summary>getMultipleAccounts accepts at most 100 keys per call.</summary>
    public int      BatchSize { get; set; } = 100;
}
