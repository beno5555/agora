using colosseumbackend.Common;
using colosseumbackend.Data.Entities;

namespace colosseumbackend.Services;

/// <summary>
/// Which actions the caller may take right now. Mirrors the program's checks so the UI only
/// offers buttons that will succeed; the program still enforces every rule itself.
/// </summary>
public static class DealRules
{
    public static List<DealAction> AllowedActions(Deal deal, string wallet, DateTime nowUtc)
    {
        var  actions   = new List<DealAction>();
        bool isBuyer   = deal.BuyerWallet == wallet;
        bool isSeller  = deal.SellerWallet == wallet;
        bool deadlinePassed = deal.DeliveryDeadline is { } deadline && nowUtc > deadline;
        bool canRefund      = deal.DeliveryDeadline is { } refundFrom && nowUtc > refundFrom + Constants.ChainClockGrace;
        bool hasOfferFromOther = deal.SplitOfferBy is not null && deal.SplitOfferBy != wallet;

        switch (deal.Status)
        {
            case DealStatus.AwaitingSeller:
                if (isBuyer)  { actions.Add(DealAction.Cancel); }
                if (isSeller) { actions.AddRange([DealAction.Accept, DealAction.SellerRefund]); }
                break;

            case DealStatus.Active:
                if (isBuyer)
                {
                    actions.AddRange([DealAction.Confirm, DealAction.RaiseDispute]);
                    if (canRefund)
                    {
                        actions.Add(DealAction.RefundUndelivered);
                    }
                }
                if (isSeller)
                {
                    if (!deadlinePassed)
                    {
                        actions.Add(DealAction.MarkDelivered);
                    }
                    actions.AddRange([DealAction.RaiseDispute, DealAction.SellerRefund]);
                }
                break;

            case DealStatus.Delivered:
                if (isBuyer)  { actions.AddRange([DealAction.Confirm, DealAction.RaiseDispute]); }
                if (isSeller) { actions.AddRange([DealAction.RaiseDispute, DealAction.SellerRefund]); }
                break;

            case DealStatus.Disputed:
                if (isBuyer || isSeller)
                {
                    actions.Add(DealAction.ProposeSplit);
                    if (hasOfferFromOther)
                    {
                        actions.Add(DealAction.AcceptSplit);
                    }
                }
                if (isBuyer)  { actions.Add(DealAction.Confirm); }
                if (isSeller) { actions.Add(DealAction.SellerRefund); }
                break;
        }

        return actions;
    }

    public static long RemainingPrice(Deal deal)
    {
        long unitPrice = deal.Price / deal.Milestones;
        return deal.Price - unitPrice * deal.MilestonesReleased;
    }
}
