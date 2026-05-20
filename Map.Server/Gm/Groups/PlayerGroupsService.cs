using Map.Server.Entities;
using Map.Server.Gm.Config;
using Microsoft.Extensions.Logging;

namespace Map.Server.Gm.Groups;

/// <summary>
/// Default <see cref="IPlayerGroupsService"/>. Thin forwarder onto
/// <see cref="IPlayerGroupConfig"/> using the PC's GroupId. The
/// session-keyed <see cref="IPermissionService"/> remains the
/// preferred entry for handler code; this service exists so
/// scripts + atcommands that take a PlayerEntity argument have a
/// single named call.
/// </summary>
public sealed class PlayerGroupsService : IPlayerGroupsService
{
    private readonly IPlayerGroupConfig _groups;
    private readonly ILogger<PlayerGroupsService> _logger;

    public PlayerGroupsService(IPlayerGroupConfig groups, ILogger<PlayerGroupsService> logger)
    {
        _groups = groups;
        _logger = logger;
    }

    public bool HasPermission(PlayerEntity pc, string permName)
    {
        if (Enum.TryParse<PcPermission>(permName, ignoreCase: true, out var perm))
            return _groups.HasPermission(pc.GroupId, perm);
        return false;
    }

    public bool CanUseCommand(PlayerEntity pc, string commandName)
        => _groups.CanUseAtCommand(pc.GroupId, commandName);

    public bool ShouldLogCommands(PlayerEntity pc)
    {
        // groups.yml flag — surfaces via IPlayerGroupConfig once a
        // ShouldLogCommands accessor lands. Default true so commands
        // log.
        return true;
    }

    public void PcLoad(PlayerEntity pc, int groupId) => pc.GroupId = groupId;

    public void Reload() { /* IPlayerGroupConfig owns the reload pulse */ }
}
