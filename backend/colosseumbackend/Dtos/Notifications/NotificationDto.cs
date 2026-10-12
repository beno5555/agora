using colosseumbackend.Common;

namespace colosseumbackend.Dtos;

public class NotificationDto
{
    public long             Id        { get; set; }
    public Guid?            DealId    { get; set; }
    public NotificationKind Kind      { get; set; }
    public DateTime         CreatedAt { get; set; }
    public bool             IsRead    { get; set; }
}
