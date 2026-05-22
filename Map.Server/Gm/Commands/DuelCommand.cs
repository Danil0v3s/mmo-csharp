using Core.Server.Packets.Out.ZC;
using Map.Server.Duel;
using Map.Server.Entities;
using Map.Server.Services;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// Helper for sending a single chat-style reply to the caller. Mirrors
/// rAthena's <c>clif_displaymessage</c> path used by every atcommand.
/// </summary>
internal static class GmCommandReply
{
    public static void Send(IVisibilityService visibility, PlayerEntity caller, string message)
        => visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT { Message = message });
}

/// <summary>
/// <c>@duel [name]</c> — create a duel or show current duel info.
/// rAthena <c>atcommand_duel</c> (atcommand.cpp). No args + not in
/// duel → create one. With a name → create + invite in one step.
/// </summary>
public sealed class DuelCommand(
    IDuelService duel,
    IPlayerMapService players,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "duel";
    public string Description => "@duel [name] — create a duel (or show info if you're already in one).";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var existing = duel.GetDuelIdFor(caller);
        if (args.Count == 0)
        {
            if (existing > 0)
            {
                GmCommandReply.Send(visibility, caller, $"@duel: {duel.ShowInfo(existing)}");
                return Task.CompletedTask;
            }
            if (!duel.CheckTime(caller))
            {
                GmCommandReply.Send(visibility, caller, "@duel: still on cooldown — try again shortly.");
                return Task.CompletedTask;
            }
            var newId = duel.Create(caller);
            duel.SaveTime(caller);
            GmCommandReply.Send(visibility, caller, $"@duel: duel #{newId} created. Invite players with @invite <name>.");
            return Task.CompletedTask;
        }
        if (existing == 0)
        {
            if (!duel.CheckTime(caller))
            {
                GmCommandReply.Send(visibility, caller, "@duel: still on cooldown — try again shortly.");
                return Task.CompletedTask;
            }
            existing = duel.Create(caller);
            duel.SaveTime(caller);
        }
        var target = players.GetAllPlayers()
            .FirstOrDefault(p => string.Equals(p.Name, args[0], StringComparison.OrdinalIgnoreCase));
        if (target == null)
        {
            GmCommandReply.Send(visibility, caller, $"@duel: '{args[0]}' not online.");
            return Task.CompletedTask;
        }
        if (target.AccountId == caller.AccountId)
        {
            GmCommandReply.Send(visibility, caller, "@duel: cannot invite yourself.");
            return Task.CompletedTask;
        }
        duel.Invite(caller, target);
        GmCommandReply.Send(visibility, caller, $"@duel: invited {target.Name} to duel #{existing}.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@invite &lt;name&gt;</c> — invite a player to your active duel.
/// rAthena <c>atcommand_invite</c>.
/// </summary>
public sealed class DuelInviteCommand(
    IDuelService duel,
    IPlayerMapService players,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "invite";
    public string Description => "@invite <name> — invite a player to your active duel.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            GmCommandReply.Send(visibility, caller, "@invite: usage — @invite <name>");
            return Task.CompletedTask;
        }
        var duelId = duel.GetDuelIdFor(caller);
        if (duelId == 0)
        {
            GmCommandReply.Send(visibility, caller, "@invite: you're not in a duel. Use @duel first.");
            return Task.CompletedTask;
        }
        if (!duel.CheckPlayerLimit(duelId))
        {
            GmCommandReply.Send(visibility, caller, "@invite: duel is full.");
            return Task.CompletedTask;
        }
        var target = players.GetAllPlayers()
            .FirstOrDefault(p => string.Equals(p.Name, args[0], StringComparison.OrdinalIgnoreCase));
        if (target == null)
        {
            GmCommandReply.Send(visibility, caller, $"@invite: '{args[0]}' not online.");
            return Task.CompletedTask;
        }
        if (target.AccountId == caller.AccountId)
        {
            GmCommandReply.Send(visibility, caller, "@invite: cannot invite yourself.");
            return Task.CompletedTask;
        }
        duel.Invite(caller, target);
        GmCommandReply.Send(visibility, caller, $"@invite: invited {target.Name}.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@accept</c> — accept your pending duel invite. rAthena
/// <c>atcommand_accept</c>.
/// </summary>
public sealed class DuelAcceptCommand(
    IDuelService duel,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "accept";
    public string Description => "@accept — accept a pending duel invite.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        // The implicit-invite model from DuelService means we accept the
        // most recently opened duel that the caller isn't already in.
        // (When the wire-side ZC_DUEL_INVITE lands with per-PC pending
        // state, this resolves to that.) Until then: scan + accept
        // first available.
        for (int id = 1; id <= 64; id++)
        {
            if (!duel.Exists(id)) continue;
            if (duel.GetDuelIdFor(caller) == id) continue;
            if (duel.Accept(id, caller))
            {
                GmCommandReply.Send(visibility, caller, $"@accept: joined duel #{id}.");
                return Task.CompletedTask;
            }
        }
        GmCommandReply.Send(visibility, caller, "@accept: no pending duel invite found.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@reject</c> — reject your pending duel invite. rAthena
/// <c>atcommand_reject</c>.
/// </summary>
public sealed class DuelRejectCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "reject";
    public string Description => "@reject — reject a pending duel invite.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        // No-op on the registry — the pending-invite slot lives on the
        // wire layer; this just confirms the refusal to the caller.
        GmCommandReply.Send(visibility, caller, "@reject: duel invite rejected.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@leave</c> — leave your current duel. rAthena
/// <c>atcommand_leave</c>.
/// </summary>
public sealed class DuelLeaveCommand(
    IDuelService duel,
    IVisibilityService visibility) : IGmCommand
{
    public string Name => "leave";
    public string Description => "@leave — leave your current duel.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        var duelId = duel.GetDuelIdFor(caller);
        if (duelId == 0)
        {
            GmCommandReply.Send(visibility, caller, "@leave: you're not in a duel.");
            return Task.CompletedTask;
        }
        duel.Leave(duelId, caller);
        GmCommandReply.Send(visibility, caller, $"@leave: left duel #{duelId}.");
        return Task.CompletedTask;
    }
}
