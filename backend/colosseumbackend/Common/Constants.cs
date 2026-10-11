namespace colosseumbackend.Common;

public static class Constants
{
    #region Users
    public const int WalletMaxLength      = 44;
    public const int DisplayNameMaxLength = 40;
    public const int DiscordMaxLength     = 40;
    #endregion

    #region Listings
    public const int TitleMaxLength       = 100;
    public const int DescriptionMaxLength = 2000;
    public const int UnitLabelMaxLength   = 20;
    public const int MaxMilestones        = 100;
    public const int GameSlugMaxLength    = 40;
    public const int GameNameMaxLength    = 60;
    public const int UrlMaxLength         = 300;

    /// <summary>Delivery deadline presets a seller can pick, keyed by the value the API accepts.</summary>
    public static readonly IReadOnlyDictionary<string, TimeSpan> DeliveryWindowPresets = new Dictionary<string, TimeSpan>
    {
        ["1h"]  = TimeSpan.FromHours(1),
        ["6h"]  = TimeSpan.FromHours(6),
        ["24h"] = TimeSpan.FromHours(24),
        ["3d"]  = TimeSpan.FromDays(3),
    };

    public const string DefaultDeliveryWindow = "24h";
    #endregion

    #region Deals
    public const int BuyerNoteMaxLength = 300;
    public const int SignatureMaxLength = 88;

    /// <summary>Drafts whose create_deal transaction never landed are discarded after this.</summary>
    public static readonly TimeSpan DraftLifetime = TimeSpan.FromMinutes(30);

    public static readonly TimeSpan DeadlineReminderLead = TimeSpan.FromHours(1);
    #endregion

    #region Persistence
    public const int EnumColumnMaxLength = 30;
    public const int LogActionMaxLength  = 100;
    public const int LogEntityMaxLength  = 50;
    public const int LogDetailsMaxLength = 500;
    #endregion

    #region Paging
    public const int DefaultPageSize = 20;
    public const int MaxPageSize     = 100;
    #endregion

    #region Auth
    public const string SessionCookieName = "agora_session";
    public const int    NonceBytes        = 16;
    #endregion
}
