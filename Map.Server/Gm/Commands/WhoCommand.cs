using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Services;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@who [pattern]</c> — list every online player (name + map).
/// rAthena <c>atcommand_who</c> (atcommand.cpp:2912). Optional substring
/// filter applies to the character name (case-insensitive). rAthena
/// also has @who2 / @who3 with extra columns; we route those aliases
/// through atcommands.yml and surface the same view here.
/// </summary>
public sealed class WhoCommand(
    IVisibilityService visibility,
    IPlayerMapService players) : IGmCommand
{
    public string Name => "who";
    public string Description => "@who [pattern] — list online players (filter by substring).";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var pattern = args.Count > 0 ? args[0] : null;
        var matched = players.GetAllPlayers()
            .Where(p => pattern == null || p.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
        {
            Message = $"@who: {matched.Count} match(es).",
        });
        foreach (var p in matched)
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
            {
                Message = $"  {p.Name} (lv {p.Level}/{p.JobLevel}) @ ({p.X},{p.Y})",
            });
        }
        return Task.CompletedTask;
    }
}
