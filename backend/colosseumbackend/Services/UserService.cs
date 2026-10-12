using colosseumbackend.Common;
using colosseumbackend.Data.Entities;
using colosseumbackend.Dtos;
using colosseumbackend.Logging;
using colosseumbackend.Mappers;
using colosseumbackend.Repositories;
using colosseumbackend.Solana;
using Solnet.Wallet;

namespace colosseumbackend.Services;

public class UserService : BaseService
{
    private readonly UserRepository    _userRepository;
    private readonly ListingRepository _listingRepository;
    private readonly SolanaGateway     _solana;

    public UserService(UserRepository userRepository, ListingRepository listingRepository, SolanaGateway solana, DatabaseLogger dbLogger) : base(dbLogger)
    {
        _userRepository    = userRepository;
        _listingRepository = listingRepository;
        _solana            = solana;
    }

    public async Task<ApplicationResponse<FullUserDto>> GetMeAsync(string wallet)
    {
        var response = new ApplicationResponse<FullUserDto>();
        var user     = await _userRepository.GetByWalletAsync(wallet);

        if (user is not null)
        {
            response.Ok(UserMapper.ToFull(user), ResponseMessages.UserRetrieved);
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
        }

        return response;
    }

    public async Task<ApplicationResponse<FullUserDto>> UpdateMeAsync(string wallet, EditUserDto editUserDto)
    {
        var response = new ApplicationResponse<FullUserDto>();
        var user     = await _userRepository.GetByWalletAsync(wallet);

        if (user is not null)
        {
            user.DisplayName = string.IsNullOrWhiteSpace(editUserDto.DisplayName) ? null : editUserDto.DisplayName.Trim();
            user.Discord     = string.IsNullOrWhiteSpace(editUserDto.Discord) ? null : editUserDto.Discord.Trim();
            await _userRepository.SaveChangesAsync();

            response.Ok(UserMapper.ToFull(user), ResponseMessages.UserUpdated);
            await LogResultAsync(response.Succeeded, nameof(UpdateMeAsync), nameof(User), null, wallet, wallet);
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
            await LogResultAsync(response.Succeeded, nameof(UpdateMeAsync), nameof(User), response.Message, wallet, wallet);
        }

        return response;
    }

    /// <summary>Public profile: reputation is read from the chain, so nobody can fake it.</summary>
    public async Task<ApplicationResponse<StandardUserDto>> GetProfileAsync(string wallet)
    {
        var response = new ApplicationResponse<StandardUserDto>();
        var user     = WalletSignIn.IsValidWallet(wallet) ? await _userRepository.GetByWalletAsync(wallet) : null;

        if (user is not null)
        {
            var record   = await _solana.GetReputationAsync(new PublicKey(wallet));
            var listings = await _listingRepository.GetActiveBySellerAsync(wallet);
            response.Ok(UserMapper.ToStandard(user, record, listings), ResponseMessages.UserRetrieved);
        }
        else
        {
            response.Fail(ResponseMessages.UserNotFound);
        }

        return response;
    }
}
