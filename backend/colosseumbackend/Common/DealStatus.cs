namespace colosseumbackend.Common;

/// <summary>
/// Draft is off-chain only (create_deal not landed yet). AwaitingSeller to Disputed mirror the
/// program's open states; the rest are final outcomes read from the DealClosed event.
/// </summary>
public enum DealStatus
{
    Draft,
    AwaitingSeller,
    Active,
    Delivered,
    Disputed,
    Completed,
    Split,
    Undelivered,
    SellerRefunded,
    Cancelled,
    Expired,
}
