using colosseumbackend.Data;
using colosseumbackend.Data.Entities;

namespace colosseumbackend.Logging;

/// <summary>
/// Writes business outcomes to the ActionLogs table. Never throws: a logging failure goes
/// to the system log and the business operation carries on.
/// </summary>
public class DatabaseLogger
{
    private readonly AgoraDbContext          _dbContext;
    private readonly ILogger<DatabaseLogger> _systemLogger;

    public DatabaseLogger(AgoraDbContext dbContext, ILogger<DatabaseLogger> systemLogger)
    {
        _dbContext    = dbContext;
        _systemLogger = systemLogger;
    }

    public async Task LogAsync(ActionLog log)
    {
        try
        {
            // A separate context would avoid flushing the caller's pending changes; services
            // always save before logging, so sharing the scoped context is safe here.
            await _dbContext.ActionLogs.AddAsync(log);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _systemLogger.LogError(ex, "Failed to write action log {Action}", log.Action);
        }
    }
}
