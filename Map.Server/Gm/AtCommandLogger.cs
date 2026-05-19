using Core.Database.Context;
using Core.Database.Entities;
using Map.Server.Entities;
using Map.Server.Gm.Config;
using Map.Server.Session;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Gm;

/// <summary>
/// EF Core-backed <see cref="IAtCommandLogger"/>. Writes to
/// <c>atcommandlog</c> exactly like rAthena's SQL logger path
/// (<c>log_atcommand</c>, log.cpp:316) — same columns, same
/// timestamp-on-insert semantics. <c>LogCommands</c> filtering happens
/// here so callers don't need to know about it.
///
/// Persistence runs through a short-lived scope to avoid keeping a
/// DbContext open across the game tick.
/// </summary>
public sealed class AtCommandLogger : IAtCommandLogger
{
    private readonly IPlayerGroupConfig _groups;
    private readonly IServiceProvider _services;
    private readonly ILogger<AtCommandLogger> _logger;

    public AtCommandLogger(
        IPlayerGroupConfig groups,
        IServiceProvider services,
        ILogger<AtCommandLogger> logger)
    {
        _groups = groups;
        _services = services;
        _logger = logger;
    }

    public void Log(MapSessionData session, PlayerEntity caller, string mapName, string commandLine)
    {
        var group = _groups.Get((int)session.GroupId);
        if (group == null || !group.LogCommands) return;

        // Truncate to schema widths (map: 11, command: 255). Length checks
        // mirror the rAthena varchars so a misbehaving alias never throws
        // an EF Core data-length error.
        if (mapName.Length > 11) mapName = mapName[..11];
        if (commandLine.Length > 255) commandLine = commandLine[..255];

        var charNameTrunc = caller.Name.Length > 25 ? caller.Name[..25] : caller.Name;

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
                db.Set<AtCommandLogEntity>().Add(new AtCommandLogEntity
                {
                    AtCommandDate = DateTime.UtcNow,
                    AccountId = caller.AccountId,
                    CharId = caller.CharacterId,
                    CharName = charNameTrunc,
                    Map = mapName,
                    Command = commandLine,
                });
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "atcommandlog write failed for {Cmd}", commandLine);
            }
        });
    }
}
