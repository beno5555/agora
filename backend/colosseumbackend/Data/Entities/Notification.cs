using colosseumbackend.Common;

namespace colosseumbackend.Data.Entities;

public class Notification
{
    public long             Id        { get; set; }
    public string           Wallet    { get; set; } = string.Empty;
    public Guid?            DealId    { get; set; }
    public NotificationKind Kind      { get; set; }
    public DateTime         CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime?        ReadAt    { get; set; } // null = unread
}
