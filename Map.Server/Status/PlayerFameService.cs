using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Status;

/// <summary>
/// Default <see cref="IPlayerFameService"/> — increments
/// <see cref="PlayerEntity.Fame"/>. Persistence is handled by the
/// existing autosave loop that flushes character state to MySQL.
/// </summary>
public sealed class PlayerFameService : IPlayerFameService
{
    private readonly ILogger<PlayerFameService> _logger;

    public PlayerFameService(ILogger<PlayerFameService> logger)
    {
        _logger = logger;
    }

    public int AddFame(PlayerEntity pc, int count)
    {
        var prev = pc.Fame;
        pc.Fame = Math.Max(0, prev + count);
        _logger.LogInformation(
            "pc_addfame: char {Char} fame {Old} → {New} (+{Delta})",
            pc.CharacterId, prev, pc.Fame, count);
        return pc.Fame;
    }
}
