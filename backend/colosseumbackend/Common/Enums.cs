namespace colosseumbackend.Common;

public enum ListingType
{
    Service,
    Item,
}

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

/// <summary>Actions a party can take on a deal; each maps to one program instruction.</summary>
public enum DealAction
{
    Accept,
    Cancel,
    MarkDelivered,
    Confirm,
    RefundUndelivered,
    RaiseDispute,
    ProposeSplit,
    AcceptSplit,
    SellerRefund,
}

public enum NotificationKind
{
    DealCreated,
    DealAccepted,
    DealDelivered,
    PartsConfirmed,
    DealDisputed,
    SplitProposed,
    DealClosed,
    DeadlineSoon,
    RefundAvailable,
}
