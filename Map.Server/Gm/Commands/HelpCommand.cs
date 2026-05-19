using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Gm.Config;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@help [command]</c> — prints the help text from
/// <c>conf/atcommands.yml</c>. rAthena <c>atcommand_help</c>
/// (atcommand.cpp:1693). No-args lists every command the caller is
/// allowed to use, just like rAthena.
/// </summary>
public sealed class HelpCommand(
    IVisibilityService visibility,
    IAtCommandConfig atCommands,
    IGmCommandRegistry registry,
    Map.Server.Status.ISessionManagerAccessor sessions) : IGmCommand
{
    public string Name => "help";
    public string Description => "@help [command] — show help; with no args lists available commands.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            var session = sessions.GetByEntityId(caller.Id);
            var allowed = registry.All()
                .Where(c => session == null || registry.CanInvoke(session, c))
                .Select(c => c.Name)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
            {
                Message = $"Available commands ({allowed.Count}): {string.Join(", ", allowed)}",
            });
            return Task.CompletedTask;
        }

        var nameQuery = args[0].TrimStart('@', '#').ToLowerInvariant();
        var entry = atCommands.Get(nameQuery);
        if (entry == null)
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
            {
                Message = $"@help: unknown command '{nameQuery}'.",
            });
            return Task.CompletedTask;
        }

        // rAthena prints the Help block line-by-line; we collapse to one
        // chat bubble per line to match the wire pattern.
        foreach (var line in entry.Help.Split('\n', StringSplitOptions.None))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = line });
        }
        if (entry.Aliases.Count > 0)
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
            {
                Message = $"Aliases: {string.Join(", ", entry.Aliases)}",
            });
        }
        return Task.CompletedTask;
    }
}
