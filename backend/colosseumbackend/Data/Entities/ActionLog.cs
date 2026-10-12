namespace colosseumbackend.Data.Entities;

public class ActionLog
{
    public long     Id        { get; set; }
    public string   Action    { get; set; } = string.Empty;
    public string   Entity    { get; set; } = string.Empty;
    public string?  EntityId  { get; set; }
    public bool     Succeeded { get; set; }
    /// <summary>Wallet of the caller; null for background jobs.</summary>
    public string?  Wallet    { get; set; }
    public string?  Details   { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
