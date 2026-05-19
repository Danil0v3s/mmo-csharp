using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Gm.Config;
using Map.Server.Visibility;
using Microsoft.Extensions.DependencyInjection;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@commands</c> — list every atcommand the caller can invoke.
/// rAthena <c>atcommand_commands_sub</c> (atcommand.cpp:7796), atype
/// = <c>COMMAND_ATCOMMAND</c>.
/// </summary>
public sealed class CommandsCommand(
    IVisibilityService visibility,
    IServiceProvider services,
    Map.Server.Status.ISessionManagerAccessor sessions) : IGmCommand
{
    public string Name => "commands";
    public string Description => "@commands — list every atcommand the caller can use.";

    // Lazy — see HelpCommand for the same DI-cycle workaround.
    private IGmCommandRegistry Registry => services.GetRequiredService<IGmCommandRegistry>();

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var session = sessions.GetByEntityId(caller.Id);
        var allowed = Registry.All()
            .Where(c => session == null || Registry.CanInvoke(session, c))
            .Select(c => c.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
        {
            Message = $"@-commands ({allowed.Count}): {string.Join(", ", allowed)}",
        });
        return Task.CompletedTask;
    }
}
