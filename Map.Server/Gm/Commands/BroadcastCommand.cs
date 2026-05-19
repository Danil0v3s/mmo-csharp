using Core.Server.Network;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Services;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// <c>@broadcast &lt;message&gt;</c> — server-wide ZC_BROADCAST2 with
/// caller's name prefix. rAthena <c>atcommand_broadcast</c>
/// (atcommand.cpp:5174). The <c>kami</c> family (kami/kamib/kamic/lkami)
/// shares this implementation via aliases routing here.
/// </summary>
public sealed class BroadcastCommand(
    IPlayerMapService players,
    Map.Server.Status.ISessionManagerAccessor sessions,
    IVisibilityService visibility
) : IGmCommand
{
    public string Name => "broadcast";
    public string Description => "@broadcast <message> — server-wide announcement (color = yellow).";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
            {
                Message = "@broadcast: usage — @broadcast <message>",
            });
            return Task.CompletedTask;
        }
        var text = $"{caller.Name} : {string.Join(' ', args)}";
        // rAthena uses BC_DEFAULT (yellow); we emit one ZC_BROADCAST2 to
        // every online PlayerEntity. Per-channel localbroadcast is the
        // local-only variant; broadcast is the world-wide one.
        BroadcastTo(players.GetAllPlayers(), text);
        return Task.CompletedTask;
    }

    /// <summary>Used by LocalBroadcastCommand too — pull-out helper.</summary>
    internal void BroadcastTo(IEnumerable<PlayerEntity> targets, string text)
    {
        var packet = new ZC_BROADCAST2
        {
            FontColor = 0xFFFF00, // BC_DEFAULT yellow
            FontType = 0,
            FontSize = 12,
            FontAlign = 0,
            FontY = 0,
            Message = text,
        };
        foreach (var p in targets)
        {
            var s = sessions.GetByEntityId(p.Id);
            s?.EnqueuePacket(packet);
        }
    }
}

/// <summary>
/// <c>@localbroadcast &lt;message&gt;</c> — same-map ZC_BROADCAST2.
/// rAthena <c>atcommand_localbroadcast</c> (atcommand.cpp:5210). Cheaper
/// than <c>@broadcast</c> for event-team narration confined to a venue.
/// </summary>
public sealed class LocalBroadcastCommand(
    IPlayerMapService players,
    Map.Server.Status.ISessionManagerAccessor sessions,
    IVisibilityService visibility
) : IGmCommand
{
    public string Name => "localbroadcast";
    public string Description => "@localbroadcast <message> — same-map announcement.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
            {
                Message = "@localbroadcast: usage — @localbroadcast <message>",
            });
            return Task.CompletedTask;
        }
        var text = $"{caller.Name} : {string.Join(' ', args)}";
        // Reuses BroadcastCommand's wire format by walking the same-map
        // player set instead of the world list.
        new BroadcastCommand(players, sessions, visibility)
            .BroadcastTo(players.GetPlayersOnMap(caller.MapId), text);
        return Task.CompletedTask;
    }
}
