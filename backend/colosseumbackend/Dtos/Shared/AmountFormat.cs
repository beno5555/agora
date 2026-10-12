namespace colosseumbackend.Dtos;

// Token amounts travel as strings of base units: u64 values don't fit safely in a JavaScript number.
public static class AmountFormat
{
    public const string Pattern = @"^\d{1,18}$"; // fits in a signed 64-bit column
}
