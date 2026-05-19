using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Gm.Config;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@charcommands</c> — list every charcommand (the <c>#</c>-prefix
/// admin-on-other-player form) the caller can use. rAthena
/// <c>atcommand_commands_sub</c> with <c>type = COMMAND_CHARCOMMAND</c>.
/// We don't ship <c>#cmd</c> dispatch yet (CharCommands list is
/// honored by the parser when it does); meanwhile the listing is
/// driven from <c>conf/groups.yml</c> so the help surface still works.
/// </summary>
public sealed class CharCommandsCommand(
    IVisibilityService visibility,
    Map.Server.Gm.Config.IPlayerGroupConfig groups,
    Map.Server.Gm.Config.IPermissionService perm,
    Map.Server.Status.ISessionManagerAccessor sessions) : IGmCommand
{
    public string Name => "charcommands";
    public string Description => "@charcommands — list every #-command (admin-on-target) the caller can use.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var session = sessions.GetByEntityId(caller.Id);
        if (session == null) return Task.CompletedTask;

        var group = groups.Get((int)session.GroupId);
        var hasAll = group?.Permissions.Contains(PcPermission.UseAllCommands) ?? false;
        var names = group == null ? Array.Empty<string>() :
            hasAll
                ? group.Commands.ToArray()
                : group.CharCommands.ToArray();
        Array.Sort(names, StringComparer.Ordinal);

        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
        {
            Message = $"#-commands ({names.Length}): {string.Join(", ", names)}",
        });
        return Task.CompletedTask;
    }
}
