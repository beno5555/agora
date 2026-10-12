namespace colosseumbackend.Dtos;

public class DeliveryWindowDto
{
    /// <summary>The value to send as a listing's deliveryWindow, e.g. "24h".</summary>
    public string Key     { get; set; } = string.Empty;
    public long   Seconds { get; set; }
}
