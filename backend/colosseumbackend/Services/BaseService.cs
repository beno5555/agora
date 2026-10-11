using colosseumbackend.Common;
using colosseumbackend.Data.Entities;
using colosseumbackend.Logging;

namespace colosseumbackend.Services;

public abstract class BaseService
{
    private readonly DatabaseLogger _dbLogger;
    private readonly string         _serviceName;

    protected BaseService(DatabaseLogger dbLogger)
    {
        _dbLogger    = dbLogger;
        _serviceName = GetType().Name;
    }

    protected async Task LogResultAsync(bool succeeded, string method, string entity, string? details, string? entityId, string? wallet)
    {
        await _dbLogger.LogAsync(new ActionLog
        {
            Action    = $"{_serviceName}.{method}",
            Entity    = entity,
            EntityId  = entityId,
            Succeeded = succeeded,
            Wallet    = wallet,
            Details   = details is null ? null : details[..Math.Min(details.Length, Constants.LogDetailsMaxLength)],
        });
    }
}
