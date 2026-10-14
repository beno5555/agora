namespace colosseumbackend.Common;

public static class ResponseMessages
{
    private const string Successfully = "Successfully";
    private const string NotFound     = "Not Found";
    private const string Retrieved    = $"Retrieved {Successfully}";

    #region Auth
    public const string NonceIssued        = $"Sign-In Message Issued {Successfully}";
    public const string SignedIn           = $"Signed In {Successfully}";
    public const string InvalidSignIn      = "Invalid Or Expired Sign-In";
    public const string InvalidWallet      = "Invalid Wallet Address";
    #endregion

    #region Users
    public const string UserRetrieved = $"User {Retrieved}";
    public const string UserUpdated   = $"User Updated {Successfully}";
    public const string UserNotFound  = $"User {NotFound}";
    #endregion

    #region Games
    public const string GamesRetrieved = $"Games {Retrieved}";
    #endregion

    #region Listings
    public const string ListingsRetrieved      = $"Listings {Retrieved}";
    public const string ListingRetrieved       = $"Listing {Retrieved}";
    public const string ListingCreated         = $"Listing Created {Successfully}";
    public const string ListingUpdated         = $"Listing Updated {Successfully}";
    public const string ListingNotFound        = $"Listing {NotFound}";
    public const string GameNotFound           = $"Game {NotFound}";
    public const string InvalidDeliveryWindow  = "Delivery Window Is Not One Of The Available Options";
    public const string PriceNotDivisible      = "Price Must Divide Evenly Into Milestones";
    public const string CannotBuyOwnListing    = "You Cannot Buy Your Own Listing";
    #endregion

    #region Deals
    public const string DealsRetrieved       = $"Deals {Retrieved}";
    public const string DealRetrieved        = $"Deal {Retrieved}";
    public const string DealCreated          = $"Deal Created {Successfully}; Sign The Transaction To Fund It";
    public const string DealNotFound         = $"Deal {NotFound}";
    public const string TransactionBuilt     = $"Transaction Built {Successfully}";
    public const string ActionNotAllowed     = "That Action Is Not Allowed Right Now";
    public const string InvalidActionInput   = "Invalid Parts Or Split Amount";
    public const string DealSynced           = $"Deal Synced {Successfully}";
    public const string TransactionNotFound  = "Transaction Not Confirmed";
    public const string ChainUnavailable     = "Solana Is Unreachable Right Now; Try Again";
    public const string IncompatibleDeal     = "This Deal Uses An Older Program Version And Cannot Be Synced";
    #endregion

    #region Notifications
    public const string NotificationsRetrieved = $"Notifications {Retrieved}";
    public const string NotificationRead       = $"Notification Marked Read {Successfully}";
    public const string NotificationNotFound   = $"Notification {NotFound}";
    #endregion

    #region Config
    public const string ConfigRetrieved = $"Config {Retrieved}";
    #endregion
}
