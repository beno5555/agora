namespace colosseumbackend.Configuration;

/// <summary>Settings for running behind a reverse proxy or tunnel. The defaults keep local runs unchanged.</summary>
public class HostingOptions
{
    public const string SectionName = "Hosting";

    /// <summary>
    /// Read the client's IP and scheme from X-Forwarded-For/-Proto. Only requests from loopback,
    /// <see cref="KnownProxies"/> or <see cref="KnownNetworks"/> are trusted, so nobody else can fake their IP.
    /// </summary>
    public bool     ForwardedHeaders { get; set; }
    /// <summary>Proxy IPs to trust besides loopback, e.g. "10.0.0.5".</summary>
    public string[] KnownProxies     { get; set; } = [];
    /// <summary>Proxy networks to trust besides loopback, in CIDR form, e.g. "10.0.0.0/8".</summary>
    public string[] KnownNetworks    { get; set; } = [];
    /// <summary>Turn off when the proxy terminates TLS and doesn't send X-Forwarded-Proto.</summary>
    public bool     HttpsRedirection { get; set; } = true;
}
