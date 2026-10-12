using System.Globalization;

namespace colosseumbackend.Mappers;

public static class AmountMapper
{
    public static string ToDto(long amount)
    {
        return amount.ToString(CultureInfo.InvariantCulture);
    }

    public static string ToDto(ulong amount)
    {
        return amount.ToString(CultureInfo.InvariantCulture);
    }
}
