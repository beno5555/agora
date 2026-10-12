namespace colosseumbackend.Dtos;

public class ConfigDto
{
    public string Cluster      { get; set; } = string.Empty;
    public string RpcUrl       { get; set; } = string.Empty;
    public string ProgramId    { get; set; } = string.Empty;
    public string Mint         { get; set; } = string.Empty;
    public int    MintDecimals { get; set; }
    /// <summary>Null until the program's config has been initialized on this cluster.</summary>
    public int?   FeeBps       { get; set; }

    public List<DeliveryWindowDto> DeliveryWindows       { get; set; } = [];
    public string                  DefaultDeliveryWindow { get; set; } = string.Empty;
}
