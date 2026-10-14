using colosseumbackend.Common;

namespace colosseumbackend.Services;

/// <summary>Which delivery deadline presets a seller can pick on the current cluster.</summary>
public static class DeliveryWindows
{
    /// <summary>The presets offered on `cluster`, shortest first. The demo presets only exist on devnet.</summary>
    public static List<KeyValuePair<string, TimeSpan>> Available(string cluster)
    {
        var presets = Constants.DeliveryWindowPresets.AsEnumerable();
        if (cluster == Constants.DemoCluster)
        {
            presets = presets.Concat(Constants.DemoDeliveryWindowPresets);
        }

        return presets.OrderBy(preset => preset.Value).ToList();
    }

    public static bool TryGet(string cluster, string key, out TimeSpan window)
    {
        var match = Available(cluster).FirstOrDefault(preset => preset.Key == key);
        window = match.Value;
        return match.Key is not null;
    }

    /// <summary>
    /// The buyer's review time after delivery for a listing with this delivery window:
    /// <see cref="Constants.ReviewWindow"/>, except that devnet demo listings (2 minutes)
    /// get the same short window, so a claim can be shown live.
    /// </summary>
    public static TimeSpan ReviewWindowFor(long deliveryWindowSeconds)
    {
        bool isDemo = Constants.DemoDeliveryWindowPresets.Values.Any(preset => (long)preset.TotalSeconds == deliveryWindowSeconds);
        return isDemo ? TimeSpan.FromSeconds(deliveryWindowSeconds) : Constants.ReviewWindow;
    }

    /// <summary>The preset key for a stored window, e.g. "24h"; any other length reads as "{seconds}s".</summary>
    public static string KeyFor(long seconds)
    {
        var match = Constants.DeliveryWindowPresets.Concat(Constants.DemoDeliveryWindowPresets)
                             .FirstOrDefault(preset => (long)preset.Value.TotalSeconds == seconds);
        return match.Key ?? $"{seconds}s";
    }
}
