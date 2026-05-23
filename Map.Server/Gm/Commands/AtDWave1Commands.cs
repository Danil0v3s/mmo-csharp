using Core.Server.Packets.Out.ZC;
using Map.Server.BattleGround;
using Map.Server.Entities;
using Map.Server.Services;
using Map.Server.Status;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

// AT-D1 wave — promote the 18 final "deferred subsystem" atcommand stubs to
// real handlers. Reference rAthena atcommand.cpp (the per-cmd line refs are
// inline). Each command lives here; the StubCommandKinds.Specs list drops
// the corresponding entry in the same commit so the GmCommandRegistry has
// exactly one binding per canonical name.

// ---------- baselevelup / mapexit2 / displaystatus ----------

/// <summary>
/// <c>@baselevelup &lt;delta&gt;</c> — alias of <c>@level</c>. rAthena
/// atcommand.cpp keeps this as a separate ACMD_FUNC so admins can grep
/// the canonical name; we route through the existing LevelCommand impl
/// so behavior is identical (clamp, recalc, full heal, stat-broadcast).
/// </summary>
public sealed class BaseLevelUpCommand(LevelCommand inner) : IGmCommand
{
    public string Name => "baselevelup";
    public string Description => "@baselevelup <delta> — adjust caller's base level (alias of @level).";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
        => inner.ExecuteAsync(caller, args, ct);
}

/// <summary>
/// <c>@mapexit2</c> — soft shutdown variant. rAthena differentiates the
/// hard <c>atcommand_mapexit</c> from <c>mapexit2</c> by adding a 30 s
/// announce + delayed shutdown. We mirror that: broadcast warning to
/// every connected player, then schedule
/// <see cref="Microsoft.Extensions.Hosting.IHostApplicationLifetime.StopApplication"/>
/// 30 s later. Cancellable only by another @mapexit2 (omitted here —
/// emergency overrides should use the system signal).
/// </summary>
public sealed class MapExit2Command(
    IVisibilityService visibility,
    IPlayerMapService players,
    ISessionManagerAccessor sessions,
    Microsoft.Extensions.Hosting.IHostApplicationLifetime lifetime) : IGmCommand
{
    public string Name => "mapexit2";
    public string Description => "@mapexit2 — announce + delayed (30s) map-server shutdown.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        const string msg = "Server: Map-server shutdown in 30 seconds.";
        foreach (var p in players.GetAllPlayers())
        {
            sessions.GetByEntityId(p.Id)?.EnqueuePacket(
                new ZC_NOTIFY_PLAYERCHAT { Message = msg });
        }
        GmCommandReply.Send(visibility, caller, "@mapexit2: announced + 30s shutdown timer armed.");
        _ = Task.Run(async () =>
        {
            try { await Task.Delay(TimeSpan.FromSeconds(30)).ConfigureAwait(false); }
            catch (TaskCanceledException) { return; }
            lifetime.StopApplication();
        }, ct);
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@displaystatus &lt;sc&gt; [flag] [tick] [val1] [val2] [val3]</c>
/// — visual-only SC packet for debug. rAthena
/// <c>atcommand_displaystatus</c> (atcommand.cpp:2727) calls
/// <c>clif_status_change</c> WITHOUT touching the status engine.
/// We emit <see cref="ZC_MSG_STATE_CHANGE3"/> (0x0983) so the client
/// renders the icon + duration; nothing changes server-side.
/// </summary>
public sealed class DisplayStatusCommand(
    IVisibilityService visibility,
    ISessionManagerAccessor sessions) : IGmCommand
{
    public string Name => "displaystatus";
    public string Description => "@displaystatus <sc> [flag] [tick] [v1] [v2] [v3] — visual-only SC.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0 || !short.TryParse(args[0], out var scIndex))
        {
            GmCommandReply.Send(visibility, caller, "@displaystatus: usage — @displaystatus <sc> [flag] [tick] [v1] [v2] [v3]");
            return Task.CompletedTask;
        }
        var flag = args.Count > 1 && byte.TryParse(args[1], out var f) ? f : (byte)1;
        var tick = args.Count > 2 && uint.TryParse(args[2], out var t) ? t : 0u;
        var v1 = args.Count > 3 && int.TryParse(args[3], out var x1) ? x1 : 0;
        var v2 = args.Count > 4 && int.TryParse(args[4], out var x2) ? x2 : 0;
        var v3 = args.Count > 5 && int.TryParse(args[5], out var x3) ? x3 : 0;

        sessions.GetByEntityId(caller.Id)?.EnqueuePacket(new ZC_MSG_STATE_CHANGE3
        {
            Index = scIndex,
            AccountId = (uint)caller.AccountId,
            State = flag,
            Tick = tick,
            TotalTick = tick,
            Val1 = v1, Val2 = v2, Val3 = v3,
        });
        GmCommandReply.Send(visibility, caller, $"@displaystatus: SC#{scIndex} state={flag} tick={tick}ms.");
        return Task.CompletedTask;
    }
}

// ---------- @who variants (6) ----------

internal static class WhoFormatter
{
    /// <summary>Format mode 1 — base @who: name + map + level.</summary>
    public static string Mode1(PlayerEntity p) =>
        $"  {p.Name} (lv {p.Level}/{p.JobLevel}) @ ({p.X},{p.Y})";

    /// <summary>Format mode 2 — @who2: + group id.</summary>
    public static string Mode2(PlayerEntity p) =>
        $"  {p.Name} (lv {p.Level}/{p.JobLevel} · group#{p.GroupId}) @ ({p.X},{p.Y})";

    /// <summary>Format mode 3 — @who3: + account/char ids.</summary>
    public static string Mode3(PlayerEntity p) =>
        $"  [aid={p.AccountId} cid={p.CharacterId}] {p.Name} (lv {p.Level}/{p.JobLevel}) @ ({p.X},{p.Y})";
}

/// <summary>
/// <c>@who2 [pattern]</c> — like <c>@who</c> but adds class id.
/// rAthena <c>atcommand.cpp:863</c> (who family branches on cmd name).
/// </summary>
public sealed class Who2Command(IVisibilityService visibility, IPlayerMapService players) : IGmCommand
{
    public string Name => "who2";
    public string Description => "@who2 [pattern] — list players (name + level + class).";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
        => WhoBase.Run(visibility, caller, players, args, WhoFormatter.Mode2, mapFilter: null, gmOnly: false, name: "@who2");
}

/// <summary>
/// <c>@who3 [pattern]</c> — like <c>@who2</c> + AID/CID columns.
/// </summary>
public sealed class Who3Command(IVisibilityService visibility, IPlayerMapService players) : IGmCommand
{
    public string Name => "who3";
    public string Description => "@who3 [pattern] — list players (full ids + class).";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
        => WhoBase.Run(visibility, caller, players, args, WhoFormatter.Mode3, mapFilter: null, gmOnly: false, name: "@who3");
}

/// <summary>
/// <c>@whomap [pattern]</c> — list players on the caller's current map.
/// </summary>
public sealed class WhoMapCommand(IVisibilityService visibility, IPlayerMapService players) : IGmCommand
{
    public string Name => "whomap";
    public string Description => "@whomap [pattern] — list players on this map (mode 1).";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
        => WhoBase.Run(visibility, caller, players, args, WhoFormatter.Mode1, mapFilter: caller.MapId, gmOnly: false, name: "@whomap");
}

/// <summary>
/// <c>@whomap2 [pattern]</c> — like @whomap with class id column.
/// </summary>
public sealed class WhoMap2Command(IVisibilityService visibility, IPlayerMapService players) : IGmCommand
{
    public string Name => "whomap2";
    public string Description => "@whomap2 [pattern] — list players on this map (mode 2).";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
        => WhoBase.Run(visibility, caller, players, args, WhoFormatter.Mode2, mapFilter: caller.MapId, gmOnly: false, name: "@whomap2");
}

/// <summary>
/// <c>@whomap3 [pattern]</c> — like @whomap with AID/CID columns.
/// </summary>
public sealed class WhoMap3Command(IVisibilityService visibility, IPlayerMapService players) : IGmCommand
{
    public string Name => "whomap3";
    public string Description => "@whomap3 [pattern] — list players on this map (mode 3).";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
        => WhoBase.Run(visibility, caller, players, args, WhoFormatter.Mode3, mapFilter: caller.MapId, gmOnly: false, name: "@whomap3");
}

/// <summary>
/// <c>@whogm [pattern]</c> — separate handler in rAthena; lists only
/// players whose group is GM-level (group_level &gt; 0). Approximation:
/// gates on <see cref="PlayerEntity.GroupId"/> != 0.
/// </summary>
public sealed class WhoGmCommand(IVisibilityService visibility, IPlayerMapService players) : IGmCommand
{
    public string Name => "whogm";
    public string Description => "@whogm [pattern] — list GM players only.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
        => WhoBase.Run(visibility, caller, players, args, WhoFormatter.Mode3, mapFilter: null, gmOnly: true, name: "@whogm");
}

internal static class WhoBase
{
    public static Task Run(
        IVisibilityService visibility,
        PlayerEntity caller,
        IPlayerMapService players,
        IReadOnlyList<string> args,
        Func<PlayerEntity, string> format,
        uint? mapFilter,
        bool gmOnly,
        string name)
    {
        var pattern = args.Count > 0 ? args[0] : null;
        var src = mapFilter.HasValue ? players.GetPlayersOnMap(mapFilter.Value) : players.GetAllPlayers();
        var matched = src
            .Where(p => !gmOnly || p.GroupId != 0)
            .Where(p => pattern == null || p.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var scope = mapFilter.HasValue ? $" on map #{mapFilter.Value}" : "";
        GmCommandReply.Send(visibility, caller, $"{name}: {matched.Count} match(es){scope}.");
        foreach (var p in matched) GmCommandReply.Send(visibility, caller, format(p));
        return Task.CompletedTask;
    }
}

// ---------- Battleground queue commands (9) ----------

internal static class BgQueueReply
{
    public static string Format((int QueueId, string Name, BgQueueState State, int Members, byte Required) q) =>
        $"  [{q.QueueId}] {q.Name}: {q.State} — {q.Members}/{q.Required * 2} members";
}

/// <summary>
/// <c>@bgsmall</c> — solo-join the small BG queue. rAthena
/// <c>bg_queue_join_solo("bgsmall", sd)</c>.
/// </summary>
public sealed class BgSmallCommand(IBattlegroundService bg, IVisibilityService visibility) : IGmCommand
{
    public string Name => "bgsmall";
    public string Description => "@bgsmall — join the small BG queue (solo).";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        bg.QueueJoinSolo(caller, "bgsmall");
        var snap = bg.GetQueueSnapshot("bgsmall");
        GmCommandReply.Send(visibility, caller, snap is not null
            ? $"@bgsmall: enrolled. {BgQueueReply.Format(snap.Value)}"
            : "@bgsmall: queue not registered.");
        return Task.CompletedTask;
    }
}

/// <summary><c>@bgmedium</c> — solo-join the medium BG queue.</summary>
public sealed class BgMediumCommand(IBattlegroundService bg, IVisibilityService visibility) : IGmCommand
{
    public string Name => "bgmedium";
    public string Description => "@bgmedium — join the medium BG queue (solo).";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        bg.QueueJoinSolo(caller, "bgmedium");
        var snap = bg.GetQueueSnapshot("bgmedium");
        GmCommandReply.Send(visibility, caller, snap is not null
            ? $"@bgmedium: enrolled. {BgQueueReply.Format(snap.Value)}"
            : "@bgmedium: queue not registered.");
        return Task.CompletedTask;
    }
}

/// <summary><c>@bglarge</c> — solo-join the large BG queue.</summary>
public sealed class BgLargeCommand(IBattlegroundService bg, IVisibilityService visibility) : IGmCommand
{
    public string Name => "bglarge";
    public string Description => "@bglarge — join the large BG queue (solo).";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        bg.QueueJoinSolo(caller, "bglarge");
        var snap = bg.GetQueueSnapshot("bglarge");
        GmCommandReply.Send(visibility, caller, snap is not null
            ? $"@bglarge: enrolled. {BgQueueReply.Format(snap.Value)}"
            : "@bglarge: queue not registered.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@bg [name]</c> — show all queues, or join the named one.
/// </summary>
public sealed class BgCommand(IBattlegroundService bg, IVisibilityService visibility) : IGmCommand
{
    public string Name => "bg";
    public string Description => "@bg [name] — list every BG queue, or join the named one.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            var all = bg.GetAllQueues();
            GmCommandReply.Send(visibility, caller, $"@bg: {all.Count} queue(s) registered.");
            foreach (var q in all) GmCommandReply.Send(visibility, caller, BgQueueReply.Format(q));
            return Task.CompletedTask;
        }
        bg.QueueJoinSolo(caller, args[0]);
        var snap = bg.GetQueueSnapshot(args[0]);
        GmCommandReply.Send(visibility, caller, snap is not null
            ? $"@bg: joined '{args[0]}'. {BgQueueReply.Format(snap.Value)}"
            : $"@bg: queue '{args[0]}' not registered.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@bgstart [name]</c> — force a queue to ACTIVE. Maps to rAthena
/// <c>bg_queue_start_battleground</c>.
/// </summary>
public sealed class BgStartCommand(IBattlegroundService bg, IVisibilityService visibility) : IGmCommand
{
    public string Name => "bgstart";
    public string Description => "@bgstart [name] — force a queue to ACTIVE (GM-only).";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var qname = args.Count > 0 ? args[0] : "bgsmall";
        var snap = bg.GetQueueSnapshot(qname);
        if (snap is null)
        {
            GmCommandReply.Send(visibility, caller, $"@bgstart: queue '{qname}' not registered.");
            return Task.CompletedTask;
        }
        bg.QueueStartBattleground(snap.Value.QueueId);
        GmCommandReply.Send(visibility, caller, $"@bgstart: queue '{qname}' → ACTIVE.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@bgend [name]</c> — force a queue to ENDED + clear roster.
/// </summary>
public sealed class BgEndCommand(IBattlegroundService bg, IVisibilityService visibility) : IGmCommand
{
    public string Name => "bgend";
    public string Description => "@bgend [name] — force a queue to ENDED (GM-only).";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var qname = args.Count > 0 ? args[0] : "bgsmall";
        var snap = bg.GetQueueSnapshot(qname);
        if (snap is null)
        {
            GmCommandReply.Send(visibility, caller, $"@bgend: queue '{qname}' not registered.");
            return Task.CompletedTask;
        }
        bg.QueueEnd(snap.Value.QueueId);
        GmCommandReply.Send(visibility, caller, $"@bgend: queue '{qname}' → ENDED + cleared.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@bgleave</c> — drop out of the current BG queue or team.
/// rAthena <c>bg_queue_leave</c> + <c>bg_team_leave</c>.
/// </summary>
public sealed class BgLeaveCommand(IBattlegroundService bg, IVisibilityService visibility) : IGmCommand
{
    public string Name => "bgleave";
    public string Description => "@bgleave — leave the current BG queue / team.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var inQueue = bg.QueueLeave(caller);
        var inTeam = bg.TeamLeave(caller, 0);
        if (inQueue || inTeam != 0)
            GmCommandReply.Send(visibility, caller, $"@bgleave: dropped from queue={inQueue} team={inTeam}.");
        else
            GmCommandReply.Send(visibility, caller, "@bgleave: not in any queue or team.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@bgleader</c> — mark the caller as queue leader (party/guild
/// multi-member join routes through the leader).
/// </summary>
public sealed class BgLeaderCommand(IBattlegroundService bg, IVisibilityService visibility) : IGmCommand
{
    public string Name => "bgleader";
    public string Description => "@bgleader — claim the current queue's leader slot.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, bg.SetQueueLeader(caller)
            ? "@bgleader: caller now queue leader."
            : "@bgleader: not enrolled in any queue.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@bginvite &lt;player&gt;</c> — invite a named player into the
/// caller's queue. rAthena <c>bg_queue_on_accept_invite</c> path.
/// </summary>
public sealed class BgInviteCommand(
    IBattlegroundService bg,
    IPlayerMapService players,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "bginvite";
    public string Description => "@bginvite <name> — pull a player into the caller's BG queue.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            GmCommandReply.Send(visibility, caller, "@bginvite: usage — @bginvite <name>");
            return Task.CompletedTask;
        }
        var qid = bg.TeamGetId(caller);
        if (qid == 0)
        {
            GmCommandReply.Send(visibility, caller, "@bginvite: caller is not in a queue.");
            return Task.CompletedTask;
        }
        var target = players.GetAllPlayers().FirstOrDefault(p =>
            string.Equals(p.Name, args[0], StringComparison.OrdinalIgnoreCase));
        if (target is null)
        {
            GmCommandReply.Send(visibility, caller, $"@bginvite: '{args[0]}' not online.");
            return Task.CompletedTask;
        }
        bg.QueueOnAcceptInvite(target, qid);
        GmCommandReply.Send(visibility, caller, $"@bginvite: invited {target.Name} to queue #{qid}.");
        return Task.CompletedTask;
    }
}
