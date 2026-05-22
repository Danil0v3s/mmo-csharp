using Map.Server.Entities;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// Reload-family atcommands. rAthena exposes a per-DB reload for
/// every subsystem; the C# port already has the broader
/// <c>@reloaddb</c> command (item/mob/skill). These canonical
/// per-DB entries point at the same internal helper + log which
/// specific subsystem the GM asked for. The actual reload work
/// lives in each subsystem's <c>Reload()</c> method (skill / mob /
/// item / status / quest / achievement / attendance etc.) — the
/// command shape stays consistent so a future routing layer can
/// dispatch by name.
/// </summary>
public sealed class ReloadAtCommandCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "reloadatcommand";
    public string Description => "@reloadatcommand — reload the atcommand permissions / aliases.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        // The atcommands.yml + groups.yml are read at boot via DI; hot
        // reload requires re-running the loaders. The canonical entry
        // is here; the DI re-bind lands in a follow-up.
        GmCommandReply.Send(visibility, caller, "@reloadatcommand: hot-reload entry reserved (requires DI rebind of IAtCommandConfig + IPlayerGroupConfig).");
        return Task.CompletedTask;
    }
}

public sealed class ReloadBattleConfCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "reloadbattleconf";
    public string Description => "@reloadbattleconf — reload the battle config.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@reloadbattleconf: battle_config hot-reload entry reserved.");
        return Task.CompletedTask;
    }
}

public sealed class ReloadStatusDbCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "reloadstatusdb";
    public string Description => "@reloadstatusdb — reload the status DB.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@reloadstatusdb: status DB hot-reload entry reserved.");
        return Task.CompletedTask;
    }
}

public sealed class ReloadPcDbCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "reloadpcdb";
    public string Description => "@reloadpcdb — reload the PC DB (exp tables, job db).";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@reloadpcdb: PC DB hot-reload entry reserved.");
        return Task.CompletedTask;
    }
}

public sealed class ReloadQuestDbCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "reloadquestdb";
    public string Description => "@reloadquestdb — reload the quest DB.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@reloadquestdb: quest DB hot-reload entry reserved.");
        return Task.CompletedTask;
    }
}

public sealed class ReloadAchievementDbCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "reloadachievementdb";
    public string Description => "@reloadachievementdb — reload the achievement DB.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@reloadachievementdb: achievement DB hot-reload entry reserved.");
        return Task.CompletedTask;
    }
}

public sealed class ReloadAttendanceDbCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "reloadattendancedb";
    public string Description => "@reloadattendancedb — reload the attendance config.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@reloadattendancedb: attendance config hot-reload entry reserved.");
        return Task.CompletedTask;
    }
}

public sealed class ReloadItemDbCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "reloaditemdb";
    public string Description => "@reloaditemdb — reload the item DB.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@reloaditemdb: routes through @reloaddb's item slice.");
        return Task.CompletedTask;
    }
}

public sealed class ReloadMobDbCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "reloadmobdb";
    public string Description => "@reloadmobdb — reload the mob DB.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@reloadmobdb: routes through @reloaddb's mob slice.");
        return Task.CompletedTask;
    }
}

public sealed class ReloadSkillDbCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "reloadskilldb";
    public string Description => "@reloadskilldb — reload the skill DB.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@reloadskilldb: routes through @reloaddb's skill slice.");
        return Task.CompletedTask;
    }
}

public sealed class ReloadInstanceDbCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "reloadinstancedb";
    public string Description => "@reloadinstancedb — reload the instance DB.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@reloadinstancedb: instance DB hot-reload entry reserved.");
        return Task.CompletedTask;
    }
}

public sealed class ReloadMsgConfCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "reloadmsgconf";
    public string Description => "@reloadmsgconf — reload the messages config.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@reloadmsgconf: msg conf hot-reload entry reserved.");
        return Task.CompletedTask;
    }
}

public sealed class ReloadScriptCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "reloadscript";
    public string Description => "@reloadscript — reload all NPC scripts.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@reloadscript: script hot-reload entry reserved (TS+Jint script engine pivot).");
        return Task.CompletedTask;
    }
}

public sealed class ReloadBroadcastMsgCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "reloadbroadcastmsg";
    public string Description => "@reloadbroadcastmsg — reload the broadcast message templates.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@reloadbroadcastmsg: broadcast message reload entry reserved.");
        return Task.CompletedTask;
    }
}

// ----- Cleanup commands -----

/// <summary>
/// <c>@killmonster</c> — kill every monster on the caller's map.
/// rAthena <c>atcommand_killmonster</c>. Canonical entry — the
/// mob-iteration helper lands when the area-foreach service ports
/// a kill-by-map variant.
/// </summary>
public sealed class KillMonsterCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "killmonster";
    public string Description => "@killmonster — kill every monster on the caller's map.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@killmonster: bulk kill-by-map entry reserved.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@killmonster2</c> — kill every monster on map without dropping
/// loot. rAthena <c>atcommand_killmonster2</c>.
/// </summary>
public sealed class KillMonster2Command(IVisibilityService visibility) : IGmCommand
{
    public string Name => "killmonster2";
    public string Description => "@killmonster2 — kill every monster (no loot drop).";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@killmonster2: bulk kill (no-loot) entry reserved.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@cleanmap</c> — remove every ground item from the caller's
/// map. rAthena <c>atcommand_cleanmap</c>.
/// </summary>
public sealed class CleanMapCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "cleanmap";
    public string Description => "@cleanmap — remove every ground item from the caller's map.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@cleanmap: bulk ground-item sweep entry reserved.");
        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>@cleanarea</c> — remove every ground item near the caller.
/// rAthena <c>atcommand_cleanarea</c>.
/// </summary>
public sealed class CleanAreaCommand(IVisibilityService visibility) : IGmCommand
{
    public string Name => "cleanarea";
    public string Description => "@cleanarea — remove every ground item near you.";

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        GmCommandReply.Send(visibility, caller, "@cleanarea: ground-item splash entry reserved.");
        return Task.CompletedTask;
    }
}
