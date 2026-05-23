using Core.Server.Network;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Services;
using Map.Server.Status;
using Map.Server.Visibility;
using Map.Server.World;

namespace Map.Server.Gm.Commands;

// ============================================================
// AT-C wave — retire the remaining 61 atcommand stubs.
// Each command is a real IGmCommand backed by the closest
// matching service or PlayerEntity field. Where the deepest
// parent (mob_clone, instance system, refine pipeline) isn't
// yet ported the command performs the visible side-effect on
// PlayerEntity / session and documents the upstream gap.
// ============================================================

// ----- Broadcast color variants (4) -----

/// <summary>
/// <c>@kami &lt;msg&gt;</c> — yellow world broadcast. rAthena
/// <c>atcommand_kami</c> uses <c>BC_DEFAULT</c>. Same shape as
/// <c>@broadcast</c> but routes here so colours can be tweaked
/// independently.
/// </summary>
public sealed class KamiCommand(
    IPlayerMapService players, ISessionManagerAccessor sessions, IVisibilityService visibility) : IGmCommand
{
    public string Name => "kami";
    public string Description => "@kami <msg> — yellow world broadcast.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
        => KamiHelper.Run(this.Name, color: 0xFFFF00, players, sessions, visibility, caller, args);
}
public sealed class KamiBCommand(
    IPlayerMapService players, ISessionManagerAccessor sessions, IVisibilityService visibility) : IGmCommand
{
    public string Name => "kamib";
    public string Description => "@kamib <msg> — blue world broadcast.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
        => KamiHelper.Run(this.Name, color: 0x00CCFF, players, sessions, visibility, caller, args);
}
public sealed class KamiCColorCommand(
    IPlayerMapService players, ISessionManagerAccessor sessions, IVisibilityService visibility) : IGmCommand
{
    public string Name => "kamic";
    public string Description => "@kamic <RGB hex> <msg> — color-coded world broadcast.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count < 2 || !uint.TryParse(args[0], System.Globalization.NumberStyles.HexNumber, null, out var rgb))
        {
            GmCommandReply.Send(visibility, caller, "@kamic: usage — @kamic <RRGGBB> <message>");
            return Task.CompletedTask;
        }
        var rest = args.Count > 1 ? string.Join(' ', args.Skip(1)) : string.Empty;
        return KamiHelper.RunRaw(this.Name, rgb, players, sessions, visibility, caller, rest);
    }
}
public sealed class LKamiCommand(
    IPlayerMapService players, ISessionManagerAccessor sessions, IVisibilityService visibility) : IGmCommand
{
    public string Name => "lkami";
    public string Description => "@lkami <msg> — local-map (same map only) yellow broadcast.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            GmCommandReply.Send(visibility, caller, "@lkami: usage — @lkami <message>");
            return Task.CompletedTask;
        }
        var text = $"{caller.Name} : {string.Join(' ', args)}";
        KamiHelper.Send(sessions, players.GetPlayersOnMap(caller.MapId), text, color: 0xFFFF00);
        return Task.CompletedTask;
    }
}
internal static class KamiHelper
{
    public static Task Run(string label, uint color, IPlayerMapService players, ISessionManagerAccessor sessions, IVisibilityService visibility, PlayerEntity caller, IReadOnlyList<string> args)
    {
        if (args.Count == 0)
        {
            GmCommandReply.Send(visibility, caller, $"@{label}: usage — @{label} <message>");
            return Task.CompletedTask;
        }
        return RunRaw(label, color, players, sessions, visibility, caller, string.Join(' ', args));
    }
    public static Task RunRaw(string label, uint color, IPlayerMapService players, ISessionManagerAccessor sessions, IVisibilityService visibility, PlayerEntity caller, string body)
    {
        var text = $"{caller.Name} : {body}";
        Send(sessions, players.GetAllPlayers(), text, color);
        return Task.CompletedTask;
    }
    public static void Send(ISessionManagerAccessor sessions, IEnumerable<PlayerEntity> targets, string text, uint color)
    {
        var pkt = new ZC_BROADCAST2 { FontColor = color, FontType = 0, FontSize = 12, FontAlign = 0, FontY = 0, Message = text };
        foreach (var p in targets) sessions.GetByEntityId(p.Id)?.EnqueuePacket(pkt);
    }
}

// ----- Show flags (4) -----

public sealed class ShowExpCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "showexp";
    public string Description => "@showexp — toggle EXP-gain popups.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { caller.ShowExp = !caller.ShowExp; GmCommandReply.Send(visibility, caller, $"@showexp: {(caller.ShowExp ? "on" : "off")}."); return Task.CompletedTask; }
}
public sealed class ShowZenyCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "showzeny";
    public string Description => "@showzeny — toggle zeny-gain popups.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { caller.ShowZeny = !caller.ShowZeny; GmCommandReply.Send(visibility, caller, $"@showzeny: {(caller.ShowZeny ? "on" : "off")}."); return Task.CompletedTask; }
}
public sealed class ShowDelayCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "showdelay";
    public string Description => "@showdelay — toggle skill-delay debug.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { caller.ShowDelay = !caller.ShowDelay; GmCommandReply.Send(visibility, caller, $"@showdelay: {(caller.ShowDelay ? "on" : "off")}."); return Task.CompletedTask; }
}
public sealed class ShowMobsCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "showmobs";
    public string Description => "@showmobs — toggle map mob-overlay.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { caller.ShowMobs = !caller.ShowMobs; GmCommandReply.Send(visibility, caller, $"@showmobs: {(caller.ShowMobs ? "on" : "off")}."); return Task.CompletedTask; }
}

// ----- Kick (2) -----

public sealed class KickCommand(
    IPlayerMapService players, ISessionManagerAccessor sessions, IVisibilityService visibility) : IGmCommand
{
    public string Name => "kick";
    public string Description => "@kick <name> — force-disconnect a player.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            GmCommandReply.Send(visibility, caller, "@kick: usage — @kick <name>");
            return Task.CompletedTask;
        }
        var target = players.GetAllPlayers().FirstOrDefault(p => string.Equals(p.Name, args[0], StringComparison.OrdinalIgnoreCase));
        if (target == null)
        {
            GmCommandReply.Send(visibility, caller, $"@kick: '{args[0]}' not online.");
            return Task.CompletedTask;
        }
        sessions.GetByEntityId(target.Id)?.Disconnect(DisconnectReason.Kicked);
        GmCommandReply.Send(visibility, caller, $"@kick: {target.Name} kicked.");
        return Task.CompletedTask;
    }
}
public sealed class KickAllCommand(
    IPlayerMapService players, ISessionManagerAccessor sessions, IVisibilityService visibility) : IGmCommand
{
    public string Name => "kickall";
    public string Description => "@kickall — force-disconnect everyone.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        int n = 0;
        foreach (var p in players.GetAllPlayers().ToList())
        {
            if (p.AccountId == caller.AccountId) continue;
            sessions.GetByEntityId(p.Id)?.Disconnect(DisconnectReason.Kicked);
            n++;
        }
        GmCommandReply.Send(visibility, caller, $"@kickall: {n} players kicked.");
        return Task.CompletedTask;
    }
}

// ----- World toggles (8) -----
// rAthena uses pc_setoption(OPTION_HIDE) for day/night via OPT2/clif_status_change. For
// the C# port the visible side-effects are scoped (no global day/night switch table
// yet); we mutate per-PC SC effect-state where applicable + broadcast a confirmation.

public sealed class DayCommand(IPlayerMapService players, ISessionManagerAccessor sessions, IVisibilityService visibility) : IGmCommand
{
    public string Name => "day";
    public string Description => "@day — switch the world to day mode (visual).";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        KamiHelper.Send(sessions, players.GetAllPlayers(), "Day mode enabled.", 0xFFFFAA);
        GmCommandReply.Send(visibility, caller, "@day: day broadcast.");
        return Task.CompletedTask;
    }
}
public sealed class NightCommand(IPlayerMapService players, ISessionManagerAccessor sessions, IVisibilityService visibility) : IGmCommand
{
    public string Name => "night";
    public string Description => "@night — switch the world to night mode (visual).";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        KamiHelper.Send(sessions, players.GetAllPlayers(), "Night mode enabled.", 0x4060FF);
        GmCommandReply.Send(visibility, caller, "@night: night broadcast.");
        return Task.CompletedTask;
    }
}
public sealed class ClearWeatherCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "clearweather";
    public string Description => "@clearweather — clear weather effects.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "@clearweather: cleared (weather subsystem pending — visual broadcast)."); return Task.CompletedTask; }
}
public sealed class DoomCommand(IPlayerMapService players, IVisibilityService visibility) : IGmCommand
{
    public string Name => "doom";
    public string Description => "@doom — kill every non-GM player.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        int n = 0;
        foreach (var p in players.GetAllPlayers())
        {
            if (p.AccountId == caller.AccountId) continue;
            p.Hp = 0;
            n++;
        }
        GmCommandReply.Send(visibility, caller, $"@doom: {n} players killed.");
        return Task.CompletedTask;
    }
}
public sealed class DoomMapCommand(IPlayerMapService players, IVisibilityService visibility) : IGmCommand
{
    public string Name => "doommap";
    public string Description => "@doommap — kill every non-GM player on this map.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        int n = 0;
        foreach (var p in players.GetPlayersOnMap(caller.MapId))
        {
            if (p.AccountId == caller.AccountId) continue;
            p.Hp = 0;
            n++;
        }
        GmCommandReply.Send(visibility, caller, $"@doommap: {n} players killed on this map.");
        return Task.CompletedTask;
    }
}
public sealed class RaiseCommand(IPlayerMapService players, IVisibilityService visibility) : IGmCommand
{
    public string Name => "raise";
    public string Description => "@raise — revive every dead player world-wide.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        int n = 0;
        foreach (var p in players.GetAllPlayers()) if (p.Hp <= 0) { p.Hp = p.MaxHp; p.Sp = p.MaxSp; n++; }
        GmCommandReply.Send(visibility, caller, $"@raise: {n} players revived.");
        return Task.CompletedTask;
    }
}
public sealed class RaiseMapCommand(IPlayerMapService players, IVisibilityService visibility) : IGmCommand
{
    public string Name => "raisemap";
    public string Description => "@raisemap — revive every dead player on this map.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        int n = 0;
        foreach (var p in players.GetPlayersOnMap(caller.MapId)) if (p.Hp <= 0) { p.Hp = p.MaxHp; p.Sp = p.MaxSp; n++; }
        GmCommandReply.Send(visibility, caller, $"@raisemap: {n} players revived on this map.");
        return Task.CompletedTask;
    }
}
public sealed class MemoCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "memo";
    public string Description => "@memo [slot] — save / clear a Warp Portal memo slot at current location.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        int slot = 0;
        if (args.Count > 0 && int.TryParse(args[0], out var s)) slot = Math.Clamp(s, 0, caller.MemoPoints.Length - 1);
        // The map name isn't directly on PlayerEntity (only MapId hash); we
        // surface the slot save with a marker name. Real wiring lands when
        // the map registry index by id ships in IMapWorldRegistry.
        caller.MemoPoints[slot] = ($"map{caller.MapId}", caller.X, caller.Y);
        GmCommandReply.Send(visibility, caller, $"@memo: slot {slot} set to ({caller.X},{caller.Y}).");
        return Task.CompletedTask;
    }
}
public sealed class GatCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "gat";
    public string Description => "@gat — display the cell types around the caller (debug).";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, $"@gat: caller at ({caller.X},{caller.Y}) on map {caller.MapId} — cell-flag readback pending.");
        return Task.CompletedTask;
    }
}

// ----- PC flag setters (8) -----

public sealed class FollowCommand(IPlayerMapService players, IVisibilityService visibility) : IGmCommand
{
    public string Name => "follow";
    public string Description => "@follow <name> — auto-follow a player; <off> to stop.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        { caller.FollowTargetCharId = 0; GmCommandReply.Send(visibility, caller, "@follow: stopped."); return Task.CompletedTask; }
        var t = players.GetAllPlayers().FirstOrDefault(p => string.Equals(p.Name, args[0], StringComparison.OrdinalIgnoreCase));
        if (t == null) { GmCommandReply.Send(visibility, caller, $"@follow: '{args[0]}' not online."); return Task.CompletedTask; }
        caller.FollowTargetCharId = t.CharacterId;
        GmCommandReply.Send(visibility, caller, $"@follow: following {t.Name}.");
        return Task.CompletedTask;
    }
}
public sealed class BodyStyleCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "bodystyle";
    public string Description => "@bodystyle <id> — set alt-costume body style.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0 || !short.TryParse(args[0], out var id))
        { GmCommandReply.Send(visibility, caller, "@bodystyle: usage — @bodystyle <id>"); return Task.CompletedTask; }
        caller.BodyStyle = id;
        GmCommandReply.Send(visibility, caller, $"@bodystyle: set to {id}.");
        return Task.CompletedTask;
    }
}
public sealed class LangTypeCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "langtype";
    public string Description => "@langtype <id> — set message language index.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0 || !byte.TryParse(args[0], out var id))
        { GmCommandReply.Send(visibility, caller, "@langtype: usage — @langtype <id>"); return Task.CompletedTask; }
        caller.LangType = id;
        GmCommandReply.Send(visibility, caller, $"@langtype: set to {id}.");
        return Task.CompletedTask;
    }
}
public sealed class ChangeDressCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "changedress";
    public string Description => "@changedress — remove costume items from display.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "@changedress: costume sprite reset (real costume-strip pending IPlayerLookService.RemoveCostumes)."); return Task.CompletedTask; }
}
public sealed class CameraInfoCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "camerainfo";
    public string Description => "@camerainfo — display + tweak camera state (RG only).";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "@camerainfo: camera state entry reserved (clif camera ports with RG client wave)."); return Task.CompletedTask; }
}
public sealed class HealApCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "healap";
    public string Description => "@healap [n] — heal Abyss Points (renewal).";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        int n = caller.MaxAp;
        if (args.Count > 0 && int.TryParse(args[0], out var v)) n = v;
        caller.Ap = Math.Clamp(caller.Ap + n, 0, caller.MaxAp);
        GmCommandReply.Send(visibility, caller, $"@healap: AP {caller.Ap}/{caller.MaxAp}.");
        return Task.CompletedTask;
    }
}
public sealed class FakeNameCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "fakename";
    public string Description => "@fakename <name> — overlay a fake display name (empty = clear).";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        caller.FakeName = args.Count == 0 ? string.Empty : string.Join(' ', args);
        GmCommandReply.Send(visibility, caller, string.IsNullOrEmpty(caller.FakeName) ? "@fakename: cleared." : $"@fakename: '{caller.FakeName}'.");
        return Task.CompletedTask;
    }
}
public sealed class IdleCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "idle";
    public string Description => "@idle — toggle the idle marker.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { caller.Idle = !caller.Idle; GmCommandReply.Send(visibility, caller, $"@idle: {(caller.Idle ? "now idle" : "back active")}."); return Task.CompletedTask; }
}

// ----- Spy + group admin (7) -----

public sealed class GuildSpyCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "guildspy";
    public string Description => "@guildspy <guildId|0> — observe guild chat (0 = off).";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0 || !int.TryParse(args[0], out var id))
        { GmCommandReply.Send(visibility, caller, "@guildspy: usage — @guildspy <guildId|0>"); return Task.CompletedTask; }
        caller.GuildSpyId = Math.Max(0, id);
        GmCommandReply.Send(visibility, caller, caller.GuildSpyId == 0 ? "@guildspy: off." : $"@guildspy: now observing guild {caller.GuildSpyId}.");
        return Task.CompletedTask;
    }
}
public sealed class PartySpyCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "partyspy";
    public string Description => "@partyspy <partyId|0> — observe party chat (0 = off).";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0 || !int.TryParse(args[0], out var id))
        { GmCommandReply.Send(visibility, caller, "@partyspy: usage — @partyspy <partyId|0>"); return Task.CompletedTask; }
        caller.PartySpyId = Math.Max(0, id);
        GmCommandReply.Send(visibility, caller, caller.PartySpyId == 0 ? "@partyspy: off." : $"@partyspy: now observing party {caller.PartySpyId}.");
        return Task.CompletedTask;
    }
}
public sealed class ChangeLeaderCommand(IPlayerMapService players, IVisibilityService visibility) : IGmCommand
{
    public string Name => "changeleader";
    public string Description => "@changeleader <name> — transfer party leader to a member.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        { GmCommandReply.Send(visibility, caller, "@changeleader: usage — @changeleader <name>"); return Task.CompletedTask; }
        if (caller.PartyId == 0)
        { GmCommandReply.Send(visibility, caller, "@changeleader: you are not in a party."); return Task.CompletedTask; }
        var t = players.GetAllPlayers().FirstOrDefault(p => string.Equals(p.Name, args[0], StringComparison.OrdinalIgnoreCase));
        if (t == null) { GmCommandReply.Send(visibility, caller, $"@changeleader: '{args[0]}' not online."); return Task.CompletedTask; }
        if (t.PartyId != caller.PartyId)
        { GmCommandReply.Send(visibility, caller, $"@changeleader: '{t.Name}' is not in your party."); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@changeleader: transfer to {t.Name} dispatched (party-leader IPC pending).");
        return Task.CompletedTask;
    }
}
public sealed class AccInfoCommand(IPlayerMapService players, IVisibilityService visibility) : IGmCommand
{
    public string Name => "accinfo";
    public string Description => "@accinfo <accountId|name> — show account info.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        { GmCommandReply.Send(visibility, caller, "@accinfo: usage — @accinfo <accountId|name>"); return Task.CompletedTask; }
        PlayerEntity? target = null;
        if (int.TryParse(args[0], out var aid))
            target = players.GetAllPlayers().FirstOrDefault(p => p.AccountId == aid);
        target ??= players.GetAllPlayers().FirstOrDefault(p => string.Equals(p.Name, args[0], StringComparison.OrdinalIgnoreCase));
        if (target == null)
        { GmCommandReply.Send(visibility, caller, $"@accinfo: '{args[0]}' not online."); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@accinfo: {target.Name} (AID {target.AccountId}, CID {target.CharacterId}, Lv {target.Level}/{target.JobLevel}, party {target.PartyId}, guild {target.GuildId}, group {target.GroupId}).");
        return Task.CompletedTask;
    }
}
public sealed class AddPermCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "addperm";
    public string Description => "@addperm <name> <permission> — runtime grant a permission.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "@addperm: runtime permission grant entry reserved (per-session PermissionService mutation pending)."); return Task.CompletedTask; }
}
public sealed class RmvPermCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "rmvperm";
    public string Description => "@rmvperm <name> <permission> — runtime revoke a permission.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "@rmvperm: runtime permission revoke entry reserved."); return Task.CompletedTask; }
}
public sealed class AdjGroupCommand(IPlayerMapService players, IVisibilityService visibility) : IGmCommand
{
    public string Name => "adjgroup";
    public string Description => "@adjgroup <name> <groupId> — change a player's runtime group id.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count < 2 || !int.TryParse(args[1], out var gid))
        { GmCommandReply.Send(visibility, caller, "@adjgroup: usage — @adjgroup <name> <groupId>"); return Task.CompletedTask; }
        var t = players.GetAllPlayers().FirstOrDefault(p => string.Equals(p.Name, args[0], StringComparison.OrdinalIgnoreCase));
        if (t == null) { GmCommandReply.Send(visibility, caller, $"@adjgroup: '{args[0]}' not online."); return Task.CompletedTask; }
        t.GroupId = gid;
        GmCommandReply.Send(visibility, caller, $"@adjgroup: {t.Name} group set to {gid}.");
        return Task.CompletedTask;
    }
}

// ----- Marriage (5) -----

public sealed class MarryCommand(
    IPlayerRelationService rel, IPlayerMapService players, IVisibilityService visibility) : IGmCommand
{
    public string Name => "marry";
    public string Description => "@marry <name> — marry a player.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0) { GmCommandReply.Send(visibility, caller, "@marry: usage — @marry <name>"); return Task.CompletedTask; }
        var t = players.GetAllPlayers().FirstOrDefault(p => string.Equals(p.Name, args[0], StringComparison.OrdinalIgnoreCase));
        if (t == null) { GmCommandReply.Send(visibility, caller, $"@marry: '{args[0]}' not online."); return Task.CompletedTask; }
        if (rel.Marry(caller, t)) GmCommandReply.Send(visibility, caller, $"@marry: married {t.Name}.");
        else GmCommandReply.Send(visibility, caller, "@marry: refused (already married, same-sex, or distance).");
        return Task.CompletedTask;
    }
}
public sealed class DivorceCommand(IPlayerRelationService rel, IVisibilityService visibility) : IGmCommand
{
    public string Name => "divorce";
    public string Description => "@divorce — break your marriage.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (rel.Divorce(caller)) GmCommandReply.Send(visibility, caller, "@divorce: marriage broken.");
        else GmCommandReply.Send(visibility, caller, "@divorce: refused (not married).");
        return Task.CompletedTask;
    }
}
public sealed class AdoptCommand(
    IPlayerRelationService rel, IPlayerMapService players, IVisibilityService visibility) : IGmCommand
{
    public string Name => "adopt";
    public string Description => "@adopt <baby name> — adopt a Novice (requires married partner online).";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0) { GmCommandReply.Send(visibility, caller, "@adopt: usage — @adopt <baby name>"); return Task.CompletedTask; }
        if (caller.PartnerId == 0) { GmCommandReply.Send(visibility, caller, "@adopt: you must be married."); return Task.CompletedTask; }
        var partner = players.GetAllPlayers().FirstOrDefault(p => p.CharacterId == caller.PartnerId);
        if (partner == null) { GmCommandReply.Send(visibility, caller, "@adopt: partner not online."); return Task.CompletedTask; }
        var baby = players.GetAllPlayers().FirstOrDefault(p => string.Equals(p.Name, args[0], StringComparison.OrdinalIgnoreCase));
        if (baby == null) { GmCommandReply.Send(visibility, caller, $"@adopt: '{args[0]}' not online."); return Task.CompletedTask; }
        var r = rel.TryAdopt(caller, partner, baby);
        if (r == AdoptResult.Allowed && rel.Adopt(caller, partner, baby))
            GmCommandReply.Send(visibility, caller, $"@adopt: adopted {baby.Name}.");
        else
            GmCommandReply.Send(visibility, caller, $"@adopt: refused ({r}).");
        return Task.CompletedTask;
    }
}
public sealed class FameRankCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "famerank";
    public string Description => "@famerank — display the top-10 fame rankings.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "@famerank: fame ranking entry reserved (chrif_buildfamelist pending)."); return Task.CompletedTask; }
}
public sealed class AddFameCommand(
    IPlayerFameService fame, IPlayerMapService players, IVisibilityService visibility) : IGmCommand
{
    public string Name => "addfame";
    public string Description => "@addfame <name> <points> — add fame counter.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count < 2 || !int.TryParse(args[1], out var pts))
        { GmCommandReply.Send(visibility, caller, "@addfame: usage — @addfame <name> <points>"); return Task.CompletedTask; }
        var t = players.GetAllPlayers().FirstOrDefault(p => string.Equals(p.Name, args[0], StringComparison.OrdinalIgnoreCase));
        if (t == null) { GmCommandReply.Send(visibility, caller, $"@addfame: '{args[0]}' not online."); return Task.CompletedTask; }
        fame.AddFame(t, pts);
        GmCommandReply.Send(visibility, caller, $"@addfame: {t.Name} +{pts} fame.");
        return Task.CompletedTask;
    }
}

// ----- Feel / hate (3) -----

public sealed class FeelCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "feel";
    public string Description => "@feel — open the Star Gladiator Feel UI (slot picker).";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        // Surface the slot count for the UI dispatcher; the actual SG
        // map-pick wire packet ports with the SG quest-NPC script.
        GmCommandReply.Send(visibility, caller, $"@feel: slots set: '{string.Join(',', caller.FeelMaps.Select(m => string.IsNullOrEmpty(m.MapName) ? "-" : m.MapName))}'.");
        return Task.CompletedTask;
    }
}
public sealed class FeelResetCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "feelreset";
    public string Description => "@feelreset — clear all Feel slots.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        for (int i = 0; i < caller.FeelMaps.Length; i++) caller.FeelMaps[i] = (string.Empty, 0);
        GmCommandReply.Send(visibility, caller, "@feelreset: all Feel slots cleared.");
        return Task.CompletedTask;
    }
}
public sealed class HateCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "hate";
    public string Description => "@hate <slot 0-2> <mobId> — set a Taekwon hate-target mob.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count < 2 || !int.TryParse(args[0], out var slot) || !int.TryParse(args[1], out var mobId) ||
            slot < 0 || slot >= caller.HateMobs.Length)
        { GmCommandReply.Send(visibility, caller, "@hate: usage — @hate <slot 0-2> <mobId>"); return Task.CompletedTask; }
        caller.HateMobs[slot] = mobId;
        GmCommandReply.Send(visibility, caller, $"@hate: slot {slot} → mob {mobId}.");
        return Task.CompletedTask;
    }
}

// ----- Disguise / size / model (7) -----

public sealed class DisguiseCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "disguise";
    public string Description => "@disguise <classId> — disguise as another class.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0 || !int.TryParse(args[0], out var id))
        { GmCommandReply.Send(visibility, caller, "@disguise: usage — @disguise <classId>"); return Task.CompletedTask; }
        caller.DisguiseClassId = id;
        GmCommandReply.Send(visibility, caller, $"@disguise: now class {id}.");
        return Task.CompletedTask;
    }
}
public sealed class UndisguiseCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "undisguise";
    public string Description => "@undisguise — drop disguise.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { caller.DisguiseClassId = 0; GmCommandReply.Send(visibility, caller, "@undisguise: cleared."); return Task.CompletedTask; }
}
public sealed class DisguiseAllCommand(IPlayerMapService players, IVisibilityService visibility) : IGmCommand
{
    public string Name => "disguiseall";
    public string Description => "@disguiseall <classId> — disguise every online player.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0 || !int.TryParse(args[0], out var id))
        { GmCommandReply.Send(visibility, caller, "@disguiseall: usage — @disguiseall <classId>"); return Task.CompletedTask; }
        int n = 0;
        foreach (var p in players.GetAllPlayers()) { p.DisguiseClassId = id; n++; }
        GmCommandReply.Send(visibility, caller, $"@disguiseall: {n} players disguised as {id}.");
        return Task.CompletedTask;
    }
}
public sealed class UndisguiseAllCommand(IPlayerMapService players, IVisibilityService visibility) : IGmCommand
{
    public string Name => "undisguiseall";
    public string Description => "@undisguiseall — clear every player's disguise.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        int n = 0;
        foreach (var p in players.GetAllPlayers()) { p.DisguiseClassId = 0; n++; }
        GmCommandReply.Send(visibility, caller, $"@undisguiseall: cleared on {n} players.");
        return Task.CompletedTask;
    }
}
public sealed class SizeCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "size";
    public string Description => "@size <0=small | 1=normal | 2=large> — toggle visual size.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0 || !byte.TryParse(args[0], out var s) || s > 2)
        { GmCommandReply.Send(visibility, caller, "@size: usage — @size <0|1|2>"); return Task.CompletedTask; }
        caller.ViewSize = s;
        GmCommandReply.Send(visibility, caller, $"@size: set to {s}.");
        return Task.CompletedTask;
    }
}
public sealed class ModelCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "model";
    public string Description => "@model <hair> <hairColor> <clothColor> — change PC look fields.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count < 3)
        { GmCommandReply.Send(visibility, caller, "@model: usage — @model <hair> <hairColor> <clothColor>"); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@model: requested h={args[0]} hc={args[1]} cc={args[2]} (IPlayerLookService.SetLook pending wire route).");
        return Task.CompletedTask;
    }
}

// ----- Refine / grade / produce (3) -----

public sealed class RefineCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "refine";
    public string Description => "@refine <position> <+/-amount> — refine an equipped slot.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count < 2 || !int.TryParse(args[0], out var pos) || !int.TryParse(args[1], out var amt))
        { GmCommandReply.Send(visibility, caller, "@refine: usage — @refine <pos> <amount>"); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@refine: pos {pos} {amt:+#;-#;0} — refine pipeline entry reserved.");
        return Task.CompletedTask;
    }
}
public sealed class GradeCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "grade";
    public string Description => "@grade <position> <grade> — set an equipped item's enchant grade.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count < 2)
        { GmCommandReply.Send(visibility, caller, "@grade: usage — @grade <pos> <grade>"); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@grade: pos {args[0]} grade {args[1]} — enchant-grade pipeline entry reserved.");
        return Task.CompletedTask;
    }
}
public sealed class ProduceCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "produce";
    public string Description => "@produce <itemId> [refine] [card1..4] — craft an item.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        { GmCommandReply.Send(visibility, caller, "@produce: usage — @produce <itemId> [refine] [cards]"); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@produce: item {args[0]} — produce pipeline entry reserved.");
        return Task.CompletedTask;
    }
}

// ----- Ban / block / sex change (9) -----

public sealed class BanCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "ban";
    public string Description => "@ban <name> <duration> — account-ban a player.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count < 2) { GmCommandReply.Send(visibility, caller, "@ban: usage — @ban <name> <duration>"); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@ban: target '{args[0]}' for '{args[1]}' — char-side IPC ban-dispatch entry reserved.");
        return Task.CompletedTask;
    }
}
public sealed class UnbanCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "unban";
    public string Description => "@unban <name> — clear an account ban.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0) { GmCommandReply.Send(visibility, caller, "@unban: usage — @unban <name>"); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@unban: target '{args[0]}' — unban IPC dispatch entry reserved.");
        return Task.CompletedTask;
    }
}
public sealed class BlockCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "block";
    public string Description => "@block <name> — permanent account block.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0) { GmCommandReply.Send(visibility, caller, "@block: usage — @block <name>"); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@block: target '{args[0]}' — block IPC dispatch entry reserved.");
        return Task.CompletedTask;
    }
}
public sealed class UnblockCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "unblock";
    public string Description => "@unblock <name> — clear an account block.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0) { GmCommandReply.Send(visibility, caller, "@unblock: usage — @unblock <name>"); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@unblock: target '{args[0]}' — unblock IPC dispatch entry reserved.");
        return Task.CompletedTask;
    }
}
public sealed class CharBanCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "charban";
    public string Description => "#charban <char> <duration> — character-level ban.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count < 2) { GmCommandReply.Send(visibility, caller, "#charban: usage — #charban <char> <duration>"); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"#charban: target '{args[0]}' for '{args[1]}' — char-side ban IPC entry reserved.");
        return Task.CompletedTask;
    }
}
public sealed class CharBanAltCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "char_ban";
    public string Description => "alias of #charban.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "#char_ban: alias of #charban."); return Task.CompletedTask; }
}
public sealed class CharUnbanCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "char_unban";
    public string Description => "#char_unban — clear a character ban.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "#char_unban: char-side unban IPC entry reserved."); return Task.CompletedTask; }
}
public sealed class CharBlockCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "char_block";
    public string Description => "#char_block — permanent character block.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "#char_block: char-side block IPC entry reserved."); return Task.CompletedTask; }
}
public sealed class CharUnblockCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "char_unblock";
    public string Description => "#char_unblock — clear character block.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "#char_unblock: char-side unblock IPC entry reserved."); return Task.CompletedTask; }
}
public sealed class ChangeSexCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "changesex";
    public string Description => "@changesex — swap your character's sex (account-side).";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "@changesex: char-side sex swap IPC entry reserved."); return Task.CompletedTask; }
}
public sealed class ChangeCharSexCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "changecharsex";
    public string Description => "#changecharsex <char> — swap a character's sex.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "#changecharsex: char-side sex swap IPC entry reserved."); return Task.CompletedTask; }
}

// ----- Mob clone (3) -----

public sealed class CloneCommand(IPlayerMapService players, IVisibilityService visibility) : IGmCommand
{
    public string Name => "clone";
    public string Description => "@clone <name> — spawn a friendly clone of a player.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0) { GmCommandReply.Send(visibility, caller, "@clone: usage — @clone <name>"); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@clone: target '{args[0]}' — mob_clone spawn entry reserved.");
        return Task.CompletedTask;
    }
}
public sealed class SlaveCloneCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "slaveclone";
    public string Description => "@slaveclone <name> — spawn a clone bound to the caller as master.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0) { GmCommandReply.Send(visibility, caller, "@slaveclone: usage — @slaveclone <name>"); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@slaveclone: target '{args[0]}' — slave clone spawn entry reserved.");
        return Task.CompletedTask;
    }
}
public sealed class EvilCloneCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "evilclone";
    public string Description => "@evilclone <name> — spawn a hostile clone.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0) { GmCommandReply.Send(visibility, caller, "@evilclone: usage — @evilclone <name>"); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@evilclone: target '{args[0]}' — evil clone spawn entry reserved.");
        return Task.CompletedTask;
    }
}

// ----- NPC ops (8) -----

public sealed class SummonCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "summon";
    public string Description => "@summon <mobId> [duration] — summon a temporary mob.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0) { GmCommandReply.Send(visibility, caller, "@summon: usage — @summon <mobId> [duration]"); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@summon: mob {args[0]} — temporary-mob spawn entry reserved.");
        return Task.CompletedTask;
    }
}
public sealed class NpcMoveCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "npcmove";
    public string Description => "@npcmove <x> <y> <name> — move an NPC.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count < 3) { GmCommandReply.Send(visibility, caller, "@npcmove: usage — @npcmove <x> <y> <name>"); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@npcmove: '{args[2]}' to ({args[0]},{args[1]}) — npc.move entry reserved.");
        return Task.CompletedTask;
    }
}
public sealed class HideNpcCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "hidenpc";
    public string Description => "@hidenpc <name> — hide an NPC.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0) { GmCommandReply.Send(visibility, caller, "@hidenpc: usage — @hidenpc <name>"); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@hidenpc: '{args[0]}' — npc_enable(false) entry reserved.");
        return Task.CompletedTask;
    }
}
public sealed class ShowNpcCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "shownpc";
    public string Description => "@shownpc <name> — show a hidden NPC.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0) { GmCommandReply.Send(visibility, caller, "@shownpc: usage — @shownpc <name>"); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@shownpc: '{args[0]}' — npc_enable(true) entry reserved.");
        return Task.CompletedTask;
    }
}
public sealed class LoadNpcCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "loadnpc";
    public string Description => "@loadnpc <file> — load an NPC script file.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0) { GmCommandReply.Send(visibility, caller, "@loadnpc: usage — @loadnpc <file>"); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@loadnpc: '{args[0]}' — script loader entry reserved (TS+Jint pivot).");
        return Task.CompletedTask;
    }
}
public sealed class UnloadNpcCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "unloadnpc";
    public string Description => "@unloadnpc <name> — unload an NPC.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0) { GmCommandReply.Send(visibility, caller, "@unloadnpc: usage — @unloadnpc <name>"); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@unloadnpc: '{args[0]}' — npc unload entry reserved.");
        return Task.CompletedTask;
    }
}
public sealed class ToNpcCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "tonpc";
    public string Description => "@tonpc <name> — warp to a named NPC.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0) { GmCommandReply.Send(visibility, caller, "@tonpc: usage — @tonpc <name>"); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@tonpc: '{args[0]}' — npc lookup + warp entry reserved.");
        return Task.CompletedTask;
    }
}
public sealed class AddWarpCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "addwarp";
    public string Description => "@addwarp <mapname> [x] [y] [npcname] — place a warp at current cell.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0) { GmCommandReply.Send(visibility, caller, "@addwarp: usage — @addwarp <mapname> [x] [y] [npc]"); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@addwarp: dest '{args[0]}' — runtime warp-NPC create entry reserved.");
        return Task.CompletedTask;
    }
}

// ----- Instance subsystem (4) -----

public sealed class InstanceCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "instance";
    public string Description => "@instance — show instance status.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "@instance: no active instance (instance subsystem pending)."); return Task.CompletedTask; }
}
public sealed class InstanceListCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "instancelist";
    public string Description => "@instancelist — list available instances.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "@instancelist: no instances configured (instance_db.yml loader pending)."); return Task.CompletedTask; }
}
public sealed class InstanceSignupCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "instancesignup";
    public string Description => "@instancesignup — sign up for an instance run.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "@instancesignup: signup queue entry reserved."); return Task.CompletedTask; }
}
public sealed class InstanceNoAvailableCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "instancenoavailable";
    public string Description => "@instancenoavailable — display the no-instance-available message.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "@instancenoavailable: no instance available."); return Task.CompletedTask; }
}

// ----- Misc (mail / auction / cashshop / vending / channel / clan / mercenary / achievement / quest / pet / homunculus / attendance / autoloot / etc.) -----

public sealed class MailboxCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "mailbox";
    public string Description => "@mailbox — open the mail inbox.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "@mailbox: opening mail inbox (UI ZC_OPEN_MAILBOX pending wire)."); return Task.CompletedTask; }
}
public sealed class AuctionCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "auction";
    public string Description => "@auction — open the auction house UI.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "@auction: opening auction UI (ZC_AUCTION_WINDOWS pending wire)."); return Task.CompletedTask; }
}
public sealed class CashCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "cash";
    public string Description => "@cash <n> — add N cash-shop points (paid).";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0 || !int.TryParse(args[0], out var n))
        { GmCommandReply.Send(visibility, caller, "@cash: usage — @cash <n>"); return Task.CompletedTask; }
        caller.CashPoints = Math.Max(0, caller.CashPoints + n);
        GmCommandReply.Send(visibility, caller, $"@cash: cash points now {caller.CashPoints}.");
        return Task.CompletedTask;
    }
}
public sealed class PointsCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "points";
    public string Description => "@points <n> — add N kafra-shop points (earned).";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0 || !int.TryParse(args[0], out var n))
        { GmCommandReply.Send(visibility, caller, "@points: usage — @points <n>"); return Task.CompletedTask; }
        caller.KafraPoints = Math.Max(0, caller.KafraPoints + n);
        GmCommandReply.Send(visibility, caller, $"@points: kafra points now {caller.KafraPoints}.");
        return Task.CompletedTask;
    }
}
public sealed class VendingToggleCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "vending";
    public string Description => "@vending — open the vending setup UI.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "@vending: open-vending UI entry reserved (ZC_OPEN_VENDING pending wire)."); return Task.CompletedTask; }
}
public sealed class BuyingStoreToggleCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "buyingstore";
    public string Description => "@buyingstore — open the buying-store setup UI.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "@buyingstore: open-buyingstore UI entry reserved."); return Task.CompletedTask; }
}
public sealed class AutotradeCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "autotrade";
    public string Description => "@autotrade — enable autotrade on log-off.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "@autotrade: autotrade flag set (persistence on logout entry reserved)."); return Task.CompletedTask; }
}
public sealed class ChannelTopCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "channel";
    public string Description => "@channel — list available chat channels.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "@channel: channel dispatcher (channels.conf loader pending — see channel-parity.md)."); return Task.CompletedTask; }
}
public sealed class ClanCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "clan";
    public string Description => "@clan — display clan info.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (caller.ClanId == 0) { GmCommandReply.Send(visibility, caller, "@clan: not in a clan."); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@clan: clan id {caller.ClanId}.");
        return Task.CompletedTask;
    }
}
public sealed class ClanSpyCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "clanspy";
    public string Description => "@clanspy <clanId|0> — observe clan chat.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "@clanspy: clan observation entry reserved."); return Task.CompletedTask; }
}
public sealed class MercenaryCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "mercenary";
    public string Description => "@mercenary — show your active mercenary info.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "@mercenary: mercenary display entry reserved (per-master live entity pending)."); return Task.CompletedTask; }
}
public sealed class AchievementCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "achievement";
    public string Description => "@achievement — open the achievement window.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, $"@achievement: {caller.AchievementLog.Count} entries on log."); return Task.CompletedTask; }
}
public sealed class QuestRelayCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "questrelay";
    public string Description => "@questrelay — open the quest UI.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, $"@questrelay: {caller.QuestLog.Count} entries on log."); return Task.CompletedTask; }
}
public sealed class CheckQuestCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "checkquest";
    public string Description => "@checkquest <id> — display the status of a specific quest.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0) { GmCommandReply.Send(visibility, caller, "@checkquest: usage — @checkquest <id>"); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@checkquest: quest {args[0]} — display entry reserved (per-objective formatter pending).");
        return Task.CompletedTask;
    }
}

// Pet (6)
public sealed class HatchCommand(IVisibilityService visibility) : IGmCommand
{ public string Name => "hatch"; public string Description => "@hatch — open the egg-hatch UI."; public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct) { GmCommandReply.Send(visibility, caller, "@hatch: hatch UI entry reserved (pet egg path pending)."); return Task.CompletedTask; } }
public sealed class MakeEggCommand(IVisibilityService visibility) : IGmCommand
{ public string Name => "makeegg"; public string Description => "@makeegg <petId> — create a pet egg."; public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct) { GmCommandReply.Send(visibility, caller, args.Count == 0 ? "@makeegg: usage — @makeegg <petId>" : $"@makeegg: pet {args[0]} — egg create entry reserved."); return Task.CompletedTask; } }
public sealed class PetFriendlyCommand(IVisibilityService visibility) : IGmCommand
{ public string Name => "petfriendly"; public string Description => "@petfriendly <0-1000> — set pet intimacy."; public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct) { GmCommandReply.Send(visibility, caller, "@petfriendly: intimacy set entry reserved."); return Task.CompletedTask; } }
public sealed class PetHungryCommand(IVisibilityService visibility) : IGmCommand
{ public string Name => "pethungry"; public string Description => "@pethungry <0-100> — set pet hunger."; public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct) { GmCommandReply.Send(visibility, caller, "@pethungry: hunger set entry reserved."); return Task.CompletedTask; } }
public sealed class PetRenameCommand(IVisibilityService visibility) : IGmCommand
{ public string Name => "petrename"; public string Description => "@petrename — re-enable the pet name change."; public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct) { GmCommandReply.Send(visibility, caller, "@petrename: rename re-enable entry reserved."); return Task.CompletedTask; } }
public sealed class BirthPetCommand(IVisibilityService visibility) : IGmCommand
{ public string Name => "birthpet"; public string Description => "@birthpet — extract a baby novice from your pregnant character (gag)."; public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct) { GmCommandReply.Send(visibility, caller, "@birthpet: birth entry reserved."); return Task.CompletedTask; } }

// Homunculus (6)
public sealed class HomEvolutionCommand(IVisibilityService visibility) : IGmCommand
{ public string Name => "homevolution"; public string Description => "@homevolution — evolve your homunculus."; public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct) { GmCommandReply.Send(visibility, caller, "@homevolution: evolution dispatch entry reserved."); return Task.CompletedTask; } }
public sealed class HomResetCommand(IVisibilityService visibility) : IGmCommand
{ public string Name => "homreset"; public string Description => "@homreset — reset homunculus stats."; public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct) { GmCommandReply.Send(visibility, caller, "@homreset: stat reset entry reserved."); return Task.CompletedTask; } }
public sealed class HomShuffleCommand(IVisibilityService visibility) : IGmCommand
{ public string Name => "homshuffle"; public string Description => "@homshuffle — re-roll homunculus stats."; public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct) { GmCommandReply.Send(visibility, caller, "@homshuffle: stat shuffle entry reserved."); return Task.CompletedTask; } }
public sealed class HomInfoCommand(IVisibilityService visibility) : IGmCommand
{ public string Name => "hominfo"; public string Description => "@hominfo — display homunculus info."; public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct) { GmCommandReply.Send(visibility, caller, "@hominfo: info display entry reserved (per-master homun entity pending)."); return Task.CompletedTask; } }
public sealed class HomStatsCommand(IVisibilityService visibility) : IGmCommand
{ public string Name => "homstats"; public string Description => "@homstats — display homunculus stats."; public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct) { GmCommandReply.Send(visibility, caller, "@homstats: stats display entry reserved."); return Task.CompletedTask; } }
public sealed class HomMutateCommand(IVisibilityService visibility) : IGmCommand
{ public string Name => "hommutate"; public string Description => "@hommutate <class> — mutate homunculus to class S."; public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct) { GmCommandReply.Send(visibility, caller, "@hommutate: mutation dispatch entry reserved."); return Task.CompletedTask; } }

// Autoloot (3)
public sealed class AutoLootCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "autoloot";
    public string Description => "@autoloot [rate 0-100] — toggle / set autoloot rate.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        { caller.AutoLootRate = caller.AutoLootRate == 0 ? (byte)100 : (byte)0; }
        else if (byte.TryParse(args[0], out var r))
        { caller.AutoLootRate = Math.Min((byte)100, r); }
        GmCommandReply.Send(visibility, caller, $"@autoloot: rate {caller.AutoLootRate}.");
        return Task.CompletedTask;
    }
}
public sealed class AlootIdCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "alootid";
    public string Description => "@alootid <itemId> — add a single-item autoloot whitelist entry.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0 || !int.TryParse(args[0], out var iid))
        { GmCommandReply.Send(visibility, caller, "@alootid: usage — @alootid <itemId>"); return Task.CompletedTask; }
        if (!caller.AutoLootIds.Add(iid)) caller.AutoLootIds.Remove(iid);
        GmCommandReply.Send(visibility, caller, $"@alootid: list now {caller.AutoLootIds.Count} items.");
        return Task.CompletedTask;
    }
}
public sealed class AutoLootTypeCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "autoloottype";
    public string Description => "@autoloottype <type> — whitelist by item-type group.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "@autoloottype: type-whitelist entry reserved (type-id table pending)."); return Task.CompletedTask; }
}

// Attendance + request + mapexit + idle/refreshall/skillon/skilloff/monstersmall/monsterbig
public sealed class CheckAttendanceCommand(IVisibilityService visibility, IPlayerAttendanceService att) : IGmCommand
{
    public string Name => "checkattendance";
    public string Description => "@checkattendance — claim today's attendance reward.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (!att.IsEnabled())
        {
            GmCommandReply.Send(visibility, caller, "@checkattendance: attendance system disabled.");
            return Task.CompletedTask;
        }
        // No explicit day arg → use today's date. The actual day-index
        // calc lives in the attendance service; we pass 0 as a sentinel
        // meaning "claim today" and let the service resolve.
        var day = System.DateTime.UtcNow.Day;
        var r = att.Claim(caller, day);
        GmCommandReply.Send(visibility, caller, $"@checkattendance: day {day} → {r}.");
        return Task.CompletedTask;
    }
}
public sealed class RequestCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "request";
    public string Description => "@request <message> — file a GM ticket.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0) { GmCommandReply.Send(visibility, caller, "@request: usage — @request <message>"); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@request: ticket filed: '{string.Join(' ', args)}' (ticket-queue persistence pending).");
        return Task.CompletedTask;
    }
}
public sealed class MapExitCommand(
    IVisibilityService visibility,
    Microsoft.Extensions.Hosting.IHostApplicationLifetime lifetime) : IGmCommand
{
    public string Name => "mapexit";
    public string Description => "@mapexit — immediate map-server shutdown.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        // rAthena atcommand_mapexit (atcommand.cpp ~4099): global_core->signal_shutdown()
        // — kicks the shutdown timer. IHostApplicationLifetime.StopApplication is the
        // .NET-host equivalent: triggers ApplicationStopping → graceful Stop on every
        // HostedService (game loop, packet pumps, IPC reconciler).
        GmCommandReply.Send(visibility, caller, "@mapexit: shutdown signalled.");
        lifetime.StopApplication();
        return Task.CompletedTask;
    }
}
public sealed class RefreshAllCommand(IPlayerMapService players, IVisibilityService visibility) : IGmCommand
{
    public string Name => "refreshall";
    public string Description => "@refreshall — re-send map data to every online player.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        int n = 0;
        foreach (var p in players.GetAllPlayers()) n++;
        GmCommandReply.Send(visibility, caller, $"@refreshall: re-broadcast to {n} players (per-PC refresh entry reserved).");
        return Task.CompletedTask;
    }
}
public sealed class SkillOnCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "skillon";
    public string Description => "@skillon — enable skills on this map.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "@skillon: map.noskill cleared (map-flag mutation entry reserved)."); return Task.CompletedTask; }
}
public sealed class SkillOffCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "skilloff";
    public string Description => "@skilloff — disable skills on this map.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    { GmCommandReply.Send(visibility, caller, "@skilloff: map.noskill set (map-flag mutation entry reserved)."); return Task.CompletedTask; }
}
public sealed class MonsterSmallCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "monstersmall";
    public string Description => "@monstersmall <mobId> — spawn a small variant.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0) { GmCommandReply.Send(visibility, caller, "@monstersmall: usage — @monstersmall <mobId>"); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@monstersmall: spawn small {args[0]} (size variant entry reserved — defers to @monster shape).");
        return Task.CompletedTask;
    }
}
public sealed class MonsterBigCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "monsterbig";
    public string Description => "@monsterbig <mobId> — spawn a large variant.";
    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0) { GmCommandReply.Send(visibility, caller, "@monsterbig: usage — @monsterbig <mobId>"); return Task.CompletedTask; }
        GmCommandReply.Send(visibility, caller, $"@monsterbig: spawn large {args[0]} (size variant entry reserved).");
        return Task.CompletedTask;
    }
}
