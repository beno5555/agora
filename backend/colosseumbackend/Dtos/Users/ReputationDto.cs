namespace colosseumbackend.Dtos;

public class ReputationDto
{
    public uint   DealsCompleted   { get; set; }
    public uint   DealsDisputed    { get; set; }
    public uint   DealsUndelivered { get; set; }
    public string Volume           { get; set; } = "0";
}
