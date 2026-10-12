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
    public const int MaxMilestones        = 100;
    public const int GameSlugMaxLength    = 40;
    public const int GameNameMaxLength    = 60;
    public const int UrlMaxLength         = 300;

    /// <summary>
    /// Delivery deadline presets a seller can pick, keyed by the value the API accepts.
    /// All within the program's limits (60 seconds to 30 days).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, TimeSpan> DeliveryWindowPresets = new Dictionary<string, TimeSpan>
    {
        ["15m"] = TimeSpan.FromMinutes(15),
        ["30m"] = TimeSpan.FromMinutes(30),
        ["1h"]  = TimeSpan.FromHours(1),
        ["3h"]  = TimeSpan.FromHours(3),
        ["6h"]  = TimeSpan.FromHours(6),
        ["12h"] = TimeSpan.FromHours(12),
        ["24h"] = TimeSpan.FromHours(24),
        ["3d"]  = TimeSpan.FromDays(3),
        ["7d"]  = TimeSpan.FromDays(7),
    };

    /// <summary>Only on devnet: short enough to show a missed deadline live in a demo.</summary>
    public static readonly IReadOnlyDictionary<string, TimeSpan> DemoDeliveryWindowPresets = new Dictionary<string, TimeSpan>
    {
        ["2m"] = TimeSpan.FromMinutes(2),
    };

    public const string DefaultDeliveryWindow = "24h";
    public const string DemoCluster           = "devnet";
    #endregion

    #region Deals
    public const int BuyerNoteMaxLength = 300;
    public const int SignatureMaxLength = 88;

    /// <summary>Drafts whose create_deal transaction never landed are discarded after this.</summary>
    public static readonly TimeSpan DraftLifetime = TimeSpan.FromMinutes(30);

    public static readonly TimeSpan DeadlineReminderLead = TimeSpan.FromHours(1);

    /// <summary>
    /// The program checks the deadline against the cluster's clock, which can trail real time.
    /// The refund is offered only this long after the deadline, so the button never fails.
    /// </summary>
    public static readonly TimeSpan ChainClockGrace = TimeSpan.FromSeconds(15);
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

    #region Hosting
    public const string HealthCheckPath    = "/health";
    public const string FrontendIndexFile  = "index.html";
    /// <summary>Unknown API routes stay 404 instead of falling back to the frontend's index page.</summary>
    public const string ApiFallbackPattern = "/api/{**path}";
    #endregion
}
