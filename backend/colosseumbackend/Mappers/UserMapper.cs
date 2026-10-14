using colosseumbackend.Data.Entities;
using colosseumbackend.Dtos;
using colosseumbackend.Solana;

namespace colosseumbackend.Mappers;

public static class UserMapper
{
    public static MinimalUserDto ToMinimal(User user)
    {
        return new MinimalUserDto { Wallet = user.Wallet, DisplayName = user.DisplayName };
    }

    public static FullUserDto ToFull(User user)
    {
        return new FullUserDto
        {
            Wallet      = user.Wallet,
            DisplayName = user.DisplayName,
            Discord     = user.Discord,
            CreatedAt   = user.CreatedAt,
        };
    }

    public static ReputationDto ToReputation(RoleRecord record)
    {
        return new ReputationDto
        {
            DealsCompleted   = record.Completed,
            DealsDisputed    = record.Disputed,
            DealsUndelivered = record.Undelivered,
            OpenDisputes     = record.OpenDisputes,
            Volume           = AmountMapper.ToDto(record.Volume),
        };
    }

    public static StandardUserDto ToStandard(User user, ReputationAccount record, List<Listing> activeListings)
    {
        return new StandardUserDto
        {
            Wallet         = user.Wallet,
            DisplayName    = user.DisplayName,
            Discord        = user.Discord,
            Reputation     = new WalletReputationDto { AsSeller = ToReputation(record.AsSeller), AsBuyer = ToReputation(record.AsBuyer) },
            ActiveListings = activeListings.Select(ListingMapper.ToMinimal).ToList(),
        };
    }
}
