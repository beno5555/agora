using colosseumbackend.Common;
using colosseumbackend.Configuration;
using colosseumbackend.Data.Entities;
using colosseumbackend.Dtos;
using colosseumbackend.Logging;
using colosseumbackend.Mappers;
using colosseumbackend.Repositories;
using colosseumbackend.Solana;
using Microsoft.Extensions.Options;

namespace colosseumbackend.Services;

public class AuthService : BaseService
{
    private readonly AuthNonceRepository _nonceRepository;
    private readonly UserRepository      _userRepository;
    private readonly AuthOptions         _authOptions;

    public AuthService(
        AuthNonceRepository   nonceRepository,
        UserRepository        userRepository,
        IOptions<AuthOptions> authOptions,
        DatabaseLogger        dbLogger
        ) : base(dbLogger)
    {
        _nonceRepository = nonceRepository;
        _userRepository  = userRepository;
        _authOptions     = authOptions.Value;
    }

    public async Task<ApplicationResponse<NonceResponseDto>> IssueSignInMessageAsync(string wallet)
    {
        var response = new ApplicationResponse<NonceResponseDto>();

        if (WalletSignIn.IsValidWallet(wallet))
        {
            var issuedAt  = DateTime.UtcNow;
            var expiresAt = issuedAt + _authOptions.NonceLifetime;
            var message   = WalletSignIn.BuildMessage(_authOptions.Domain, wallet, issuedAt, expiresAt);

            await _nonceRepository.ReplaceAsync(new AuthNonce { Wallet = wallet, Message = message, ExpiresAt = expiresAt });
            response.Ok(new NonceResponseDto { Message = message }, ResponseMessages.NonceIssued);
        }
        else
        {
            response.Fail(ResponseMessages.InvalidWallet);
        }

        return response;
    }

    /// <summary>
    /// Checks the signed message against the pending nonce. Every failure (no nonce, expired,
    /// altered message, bad signature) returns the same message.
    /// </summary>
    public async Task<ApplicationResponse<FullUserDto>> VerifySignInAsync(VerifySignInDto verifySignInDto)
    {
        var response = new ApplicationResponse<FullUserDto>();
        var nonce    = WalletSignIn.IsValidWallet(verifySignInDto.Wallet)
            ? await _nonceRepository.TakeAsync(verifySignInDto.Wallet)
            : null;

        bool isUnexpired    = nonce is not null && nonce.ExpiresAt > DateTime.UtcNow;
        bool messageMatches = nonce is not null && nonce.Message == verifySignInDto.Message;
        bool isSigned       = isUnexpired && messageMatches
            && WalletSignIn.VerifySignature(verifySignInDto.Wallet, verifySignInDto.Message, verifySignInDto.Signature);

        if (isSigned)
        {
            var user = await _userRepository.GetOrCreateAsync(verifySignInDto.Wallet);
            response.Ok(UserMapper.ToFull(user), ResponseMessages.SignedIn);
        }
        else
        {
            response.Fail(ResponseMessages.InvalidSignIn);
            await LogResultAsync(response.Succeeded, nameof(VerifySignInAsync), nameof(User), "Sign-in rejected", null, null);
        }

        return response;
    }
}
