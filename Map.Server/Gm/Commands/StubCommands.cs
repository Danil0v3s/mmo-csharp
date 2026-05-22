using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// Catch-all for rAthena atcommands whose backend subsystem hasn't
/// ported yet. Each stub appears in the registry, passes group/permission
/// gating like a real command, and on invocation replies with a
/// well-formed "feature not yet ported — see X" message.
///
/// This lets <c>@commands</c> / <c>@help</c> surface the rAthena name +
/// help text from <c>atcommands.yml</c>; the user finds out at execute
/// time which subsystem we're still waiting on. As each backend lands,
/// the real command file moves into the registry and supersedes the
/// stub via DI ordering (last-wins for the same key).
///
/// rAthena reference paths in the comments below help reviewers know
/// what to delete here once the parent subsystem ports.
/// </summary>
internal static class StubCommandKinds
{
    /// <summary>One stub spec: command name + subsystem-pending label.</summary>
    public sealed record Spec(string Name, string Subsystem);

    /// <summary>
    /// Curated list of atcommands whose backend isn't ported yet. Listed
    /// by rAthena source file so the work to retire each stub is visible.
    /// Generated 2026-05-19; refresh when atcommands.yml diverges.
    /// </summary>
    public static readonly Spec[] Specs =
    {
        // instance.cpp (dungeon instances)
        new("instance", "instance.cpp"),
        new("instancelist", "instance.cpp"),
        new("instancesignup", "instance.cpp"),
        new("instancenoavailable", "instance.cpp"),
        // duel.cpp — retired AT-R1 (DuelCommand + DuelInviteCommand +
        // DuelAcceptCommand + DuelRejectCommand + DuelLeaveCommand)
        // mail.cpp (map-side; char side ports exist for the data plane)
        new("mailbox", "mail.cpp"),
        // auction.cpp (map-side)
        new("auction", "auction.cpp"),
        // marriage (in pc.cpp:8870 marry/divorce)
        new("marry", "pc.cpp"), new("divorce", "pc.cpp"), new("adopt", "pc.cpp"),
        new("famerank", "pc.cpp"), new("addfame", "pc.cpp"),
        // mercenary.cpp ext
        new("mercenary", "mercenary.cpp"),
        // clan.cpp map side
        new("clan", "clan.cpp"), new("clanspy", "clan.cpp"),
        // bg.cpp (battlegrounds)
        new("bgsmall", "bg.cpp"), new("bgmedium", "bg.cpp"), new("bglarge", "bg.cpp"),
        new("bg", "bg.cpp"), new("bgstart", "bg.cpp"), new("bgend", "bg.cpp"),
        new("bgleave", "bg.cpp"), new("bgleader", "bg.cpp"), new("bginvite", "bg.cpp"),
        // cashshop.cpp
        new("cash", "cashshop.cpp"), new("points", "cashshop.cpp"),
        // vending.cpp / buyingstore.cpp
        new("vending", "vending.cpp"), new("buyingstore", "buyingstore.cpp"),
        // channel.cpp
        new("channel", "channel.cpp"),
        // achievement.cpp ext
        new("achievement", "achievement.cpp"),
        // quest.cpp ext
        new("questrelay", "quest.cpp"), new("checkquest", "quest.cpp"),
        // pet ext (intimacy/friendly/hatch/etc.)
        new("hatch", "pet.cpp"), new("makeegg", "pet.cpp"),
        new("petfriendly", "pet.cpp"), new("pethungry", "pet.cpp"),
        new("petrename", "pet.cpp"), new("birthpet", "pet.cpp"),
        // homunculus ext
        new("homevolution", "homunculus.cpp"), new("homreset", "homunculus.cpp"),
        new("homshuffle", "homunculus.cpp"), new("hominfo", "homunculus.cpp"),
        new("homstats", "homunculus.cpp"), new("hommutate", "homunculus.cpp"),
        // disguise / model / size
        new("disguise", "pc.cpp:vd_view"), new("undisguise", "pc.cpp:vd_view"),
        new("disguiseall", "pc.cpp:vd_view"), new("undisguiseall", "pc.cpp:vd_view"),
        new("size", "pc.cpp:vd_view"), new("model", "pc.cpp:vd_view"),
        new("fakename", "pc.cpp:fakename"),
        // autoloot tier
        new("autoloot", "pc.cpp:autoloot"), new("alootid", "pc.cpp:autoloot"),
        new("autoloottype", "pc.cpp:autoloot"),
        // autotrade
        new("autotrade", "vending.cpp"),
        // GM information
        new("accinfo", "map_session_data"), new("addperm", "pc_groups.cpp"),
        new("rmvperm", "pc_groups.cpp"), new("adjgroup", "pc_groups.cpp"),
        // changegm retired AT-R1 (ChangeGmCommand).
        new("changeleader", "party.cpp"),
        // job change / skill grant
        new("jobchange", "pc.cpp:jobchange"), new("job", "pc.cpp:jobchange"),
        new("allskill", "pc.cpp:skills"), new("questskill", "pc.cpp:skills"),
        new("lostskill", "pc.cpp:skills"),
        // refine / grade
        new("refine", "pc.cpp:refine"), new("grade", "pc.cpp:enchantgrade"),
        new("produce", "pc.cpp:produce"),
        // bonus / stat allocation extras
        new("statall", "pc.cpp:status"), new("statsall", "pc.cpp:status"),
        new("allstats", "pc.cpp:status"), new("traitpoint", "pc.cpp:traits"),
        new("statuspoint", "pc.cpp:status"), new("skillpoint", "pc.cpp:skills"),
        // misc world toggles
        new("day", "pc.cpp:option"), new("night", "pc.cpp:option"),
        new("clearweather", "pc.cpp:weather"),
        new("doom", "pc.cpp:doom"), new("doommap", "pc.cpp:doom"),
        new("raise", "pc.cpp:raise"), new("raisemap", "pc.cpp:raise"),
        new("memo", "pc.cpp:memo"), new("gat", "pc.cpp:gat"),
        // killmonster variants
        new("killmonster", "mob.cpp"), new("killmonster2", "mob.cpp"),
        new("cleanmap", "item.cpp"), new("cleanarea", "item.cpp"),
        // option toggles
        new("option", "pc.cpp:option"), new("displaystatus", "pc.cpp:status"),
        // request system
        new("request", "pc.cpp:request"),
        // /mute / chat moderation
        new("mute", "pc.cpp:manner"), new("unmute", "pc.cpp:manner"),
        new("noask", "pc.cpp:noask"), new("noks", "pc.cpp:noks"),
        new("allowks", "pc.cpp:noks"),
        // followers / pets
        new("follow", "pc.cpp:follow"),
        // dropall / storeall / itemreset / cleargstorage etc.
        new("dropall", "pc.cpp:inventory"),
        new("storeall", "pc.cpp:inventory"),
        new("itemreset", "pc.cpp:inventory"),
        new("clearcart", "pc.cpp:cart"),
        new("clearstorage", "pc.cpp:inventory"),
        // cleargstorage retired AT-R1 (ClearGuildStorageCommand).
        // guild — partially retired AT-R1 (BreakGuildCommand,
        // GuildStorageCommand, ClearGuildStorageCommand, ChangeGmCommand,
        // GuildLevelUpCommand). guildspy/partyspy remain pending (no
        // observation backend).
        new("guildspy", "guild.cpp"),
        new("partyspy", "party.cpp"),
        // identify / repair
        new("identifyall", "pc.cpp:inventory"), new("repair", "pc.cpp:inventory"),
        new("repairall", "pc.cpp:inventory"),
        // jail
        new("jail", "pc.cpp:jail"), new("unjail", "pc.cpp:jail"),
        new("jailfor", "pc.cpp:jail"), new("jailtime", "pc.cpp:jail"),
        // map exit / stopnpc
        new("mapexit", "map.cpp"), new("reloaditemdb", "item.cpp"),
        new("reloadmobdb", "mob.cpp"), new("reloadskilldb", "skill.cpp"),
        new("reloadatcommand", "atcommand.cpp"),
        new("reloadbattleconf", "battle.cpp"), new("reloadstatusdb", "status.cpp"),
        new("reloadpcdb", "pc.cpp"), new("reloadmsgconf", "msg.cpp"),
        new("reloadscript", "npc.cpp"), new("reloadquestdb", "quest.cpp"),
        new("reloadinstancedb", "instance.cpp"), new("reloadbroadcastmsg", "channel.cpp"),
        new("reloadachievementdb", "achievement.cpp"), new("reloadattendancedb", "attendance.cpp"),
        // jobs
        new("baselevelup", "pc.cpp:joblevel"),
        // misc broadcast variants
        new("kami", "broadcast"), new("kamib", "broadcast"),
        new("kamic", "broadcast"), new("lkami", "broadcast"),
        // healap (ap = abyss points, renewal extra)
        new("healap", "pc.cpp:ap"),
        // spirit / soul ball
        new("spiritball", "pc.cpp:spiritball"), new("soulball", "pc.cpp:soulball"),
        // ban / block
        new("ban", "pc.cpp:ban"), new("unban", "pc.cpp:ban"),
        new("block", "pc.cpp:block"), new("unblock", "pc.cpp:block"),
        new("charban", "pc.cpp:ban"), new("char_ban", "pc.cpp:ban"),
        new("char_unban", "pc.cpp:ban"), new("char_block", "pc.cpp:block"),
        new("char_unblock", "pc.cpp:block"),
        new("kick", "pc.cpp:kick"), new("kickall", "pc.cpp:kick"),
        // settings / camera
        new("camerainfo", "clif.cpp:camera"),
        new("changedress", "pc.cpp:dress"),
        new("resurrect", "pc.cpp:resurrect"),
        // weight / showmsp
        new("showexp", "pc.cpp:showmsg"),
        new("showzeny", "pc.cpp:showmsg"),
        new("showdelay", "pc.cpp:showmsg"),
        new("showmobs", "mob.cpp"),
        // bodystyle
        new("bodystyle", "pc.cpp:bodystyle"),
        // language / etc
        new("langtype", "pc.cpp:lang"),
        // clone
        new("clone", "mob.cpp:clone"),
        new("slaveclone", "mob.cpp:clone"),
        new("evilclone", "mob.cpp:clone"),
        // changesex / changecharsex
        new("changesex", "pc.cpp:sex"),
        new("changecharsex", "pc.cpp:sex"),
        // feeling
        new("feelreset", "pc.cpp:feeling"),
        new("feel", "pc.cpp:feeling"),
        new("hate", "pc.cpp:feeling"),
        // summon (companion / temp)
        new("summon", "mob.cpp"),
        new("npcmove", "npc.cpp"),
        new("hidenpc", "npc.cpp"), new("shownpc", "npc.cpp"),
        new("loadnpc", "npc.cpp"), new("unloadnpc", "npc.cpp"),
        new("tonpc", "npc.cpp"),
        new("addwarp", "npc.cpp"),
        // exp / info / rate
        new("exp", "pc.cpp:exp"), new("rates", "pc.cpp:exp"),
        new("mobinfo", "mob.cpp"), new("iteminfo", "item.cpp"),
        new("whodrops", "mob.cpp"), new("whereis", "mob.cpp"),
        new("who2", "pc.cpp:who"), new("who3", "pc.cpp:who"),
        new("whomap", "pc.cpp:who"), new("whomap2", "pc.cpp:who"),
        new("whomap3", "pc.cpp:who"), new("whogm", "pc.cpp:who"),
        new("idsearch", "item.cpp"),
        new("stats", "pc.cpp:status"),
        new("itemlist", "pc.cpp:inventory"),
        new("cartlist", "pc.cpp:cart"),
        new("storagelist", "pc.cpp:storage"),
        new("mobsearch", "mob.cpp"),
        new("mapmove", "pc.cpp:setpos"),
        new("go", "pc.cpp:setpos"),
        new("idle", "pc.cpp:idle"),
        new("skillon", "map.cpp"), new("skilloff", "map.cpp"),
        new("refreshall", "pc.cpp:refresh"),
        new("monstersmall", "mob.cpp"), new("monsterbig", "mob.cpp"),
        // attendance
        new("checkattendance", "attendance.cpp"),
        // server-shutdown helpers
        new("mapexit2", "map.cpp"),
    };
}

/// <summary>
/// One stub instance per <see cref="StubCommandKinds.Spec"/>. The
/// registration loop in <c>Program.cs</c> walks the spec list and adds
/// each.
/// </summary>
public sealed class StubCommand : IGmCommand
{
    private readonly string _subsystem;
    private readonly IVisibilityService _visibility;

    public string Name { get; }
    public string Description { get; }

    public StubCommand(string name, string subsystem, IVisibilityService visibility)
    {
        Name = name;
        _subsystem = subsystem;
        _visibility = visibility;
        Description = $"@{name} — pending: {subsystem} not yet ported";
    }

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        _visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
        {
            Message = $"@{Name}: not yet implemented — {_subsystem} pending.",
        });
        return Task.CompletedTask;
    }
}
