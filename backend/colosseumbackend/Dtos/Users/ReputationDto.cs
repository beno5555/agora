namespace colosseumbackend.Dtos;

/// <summary>One wallet's record in one role, read from the chain.</summary>
public class ReputationDto
{
    public uint   DealsCompleted   { get; set; }
    public uint   DealsDisputed    { get; set; }
    public uint   DealsUndelivered { get; set; }
    public uint   OpenDisputes     { get; set; }
    public string Volume           { get; set; } = "0";
}
