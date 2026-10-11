using System.Security.Cryptography;
using System.Text;
using colosseumbackend.Common;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;

namespace colosseumbackend.Solana;

/// <summary>Builds the "Sign in with Solana" message and verifies wallet signatures over it.</summary>
public static class WalletSignIn
{
    private const int SignatureLength = 64;

    public static bool IsValidWallet(string? wallet)
    {
        return !string.IsNullOrWhiteSpace(wallet)
            && wallet.Length <= Constants.WalletMaxLength
            && PublicKey.IsValid(wallet, false);
    }

    public static string BuildMessage(string domain, string wallet, DateTime issuedAt, DateTime expiresAt)
    {
        string nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(Constants.NonceBytes)).ToLowerInvariant();

        return $"{domain} wants you to sign in with your Solana account:\n"
             + $"{wallet}\n\n"
             + "Sign in to Agora.\n\n"
             + $"Nonce: {nonce}\n"
             + $"Issued At: {issuedAt:yyyy-MM-ddTHH:mm:ss.fffZ}\n"
             + $"Expiration Time: {expiresAt:yyyy-MM-ddTHH:mm:ss.fffZ}";
    }

    /// <summary>`signatureBase58` is the 64-byte ed25519 signature the wallet returned for the UTF-8 message.</summary>
    public static bool VerifySignature(string wallet, string message, string signatureBase58)
    {
        bool isValid = false;
        try
        {
            byte[] signature = Encoders.Base58.DecodeData(signatureBase58);
            bool   hasLength = signature.Length == SignatureLength;
            if (hasLength)
            {
                isValid = new PublicKey(wallet).Verify(Encoding.UTF8.GetBytes(message), signature);
            }
        }
        catch (FormatException)
        {
            // Not base58: treated the same as a wrong signature.
        }

        return isValid;
    }
}
