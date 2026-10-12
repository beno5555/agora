namespace colosseumbackend.Common;

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
