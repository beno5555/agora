using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace colosseumbackend.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    /// <summary>The caller's wallet; only call on authorized endpoints, where the claim always exists.</summary>
    protected string GetWallet() => User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

    protected string? GetWalletIfSignedIn() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
