using colosseumbackend.Common;
using colosseumbackend.Configuration;
using colosseumbackend.Dtos;
using colosseumbackend.Services;
using colosseumbackend.Solana;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace colosseumbackend.Controllers;

[Route("api/config")]
[EnableRateLimiting(RateLimitConfig.Policies.Read)]
public class ConfigController : BaseController
{
    private readonly SolanaGateway _solana;
    private readonly SolanaOptions _solanaOptions;

    public ConfigController(SolanaGateway solana, IOptions<SolanaOptions> solanaOptions)
    {
        _solana        = solana;
        _solanaOptions = solanaOptions.Value;
    }

    [HttpGet]
    public async Task<ActionResult<ApplicationResponse<ConfigDto>>> Get()
    {
        var config   = await _solana.GetConfigAsync();
        var response = new ApplicationResponse<ConfigDto>();
        response.Ok(new ConfigDto
        {
            Cluster               = _solanaOptions.Cluster,
            RpcUrl                = _solanaOptions.RpcUrl,
            ProgramId             = _solanaOptions.ProgramId,
            Mint                  = _solanaOptions.Mint,
            MintDecimals          = _solanaOptions.MintDecimals,
            FeeBps                = config?.FeeBps,
            DeliveryWindows       = DeliveryWindows.Available(_solanaOptions.Cluster)
                .Select(preset => new DeliveryWindowDto { Key = preset.Key, Seconds = (long)preset.Value.TotalSeconds })
                .ToList(),
            DefaultDeliveryWindow = Constants.DefaultDeliveryWindow,
        }, ResponseMessages.ConfigRetrieved);

        return Ok(response);
    }
}
