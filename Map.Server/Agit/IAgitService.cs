namespace Map.Server.Agit;

/// <summary>
/// War of Emperium (WoE) state machine. Mirrors rAthena's
/// <c>agit_flag</c> / <c>agit2_flag</c> / <c>agit3_flag</c> globals
/// (guild.cpp:32+) and the start/end functions:
/// <list type="bullet">
/// <item><c>guild_agit_start</c> / <c>guild_agit_end</c> — WoE 1.0 (FE).</item>
/// <item><c>guild_agit2_start</c> / <c>guild_agit2_end</c> — WoE 2.0 (SE).</item>
/// <item><c>guild_agit3_start</c> / <c>guild_agit3_end</c> — WoE TE.</item>
/// </list>
/// Each start/end is idempotent — re-entering the same state returns
/// false without firing the NPC event. On a real transition the
/// matching <c>OnAgitStart*</c> / <c>OnAgitEnd*</c> NPC event fires
/// via <c>INpcOpsService.EventDoAll</c>.
///
/// Consumers (alliance gates, opposition gates, guild-aura selection)
/// read the per-edition <c>IsActive</c> flags; <c>IsAnyActive</c> is
/// the catch-all that matches rAthena's <c>is_agit_start() ||
/// is_agit2_start() || is_agit3_start()</c> idiom.
/// </summary>
public interface IAgitService
{
    /// <summary>rAthena <c>is_agit_start()</c> — WoE 1.0 flag.</summary>
    bool IsAgitActive { get; }
    /// <summary>rAthena <c>is_agit2_start()</c> — WoE 2.0 flag.</summary>
    bool IsAgit2Active { get; }
    /// <summary>rAthena <c>is_agit3_start()</c> — WoE TE flag.</summary>
    bool IsAgit3Active { get; }
    /// <summary>True if any WoE edition is running.</summary>
    bool IsAnyActive { get; }

    /// <summary>rAthena <c>guild_agit_start</c> (cpp:2532). Idempotent; returns false if already active.</summary>
    bool AgitStart();
    /// <summary>rAthena <c>guild_agit_end</c> (cpp:2547).</summary>
    bool AgitEnd();

    /// <summary>rAthena <c>guild_agit2_start</c> (cpp:2562).</summary>
    bool Agit2Start();
    /// <summary>rAthena <c>guild_agit2_end</c> (cpp:2577).</summary>
    bool Agit2End();

    /// <summary>rAthena <c>guild_agit3_start</c> (cpp:2592).</summary>
    bool Agit3Start();
    /// <summary>rAthena <c>guild_agit3_end</c> (cpp:2607).</summary>
    bool Agit3End();

    /// <summary>End every running WoE edition (boot / shutdown / GM @agitend all).</summary>
    void EndAll();
}

/// <summary>
/// Canonical WoE NPC event names. Mirrors rAthena
/// <c>script_config.agit_*_event_name</c> defaults from
/// <c>src/map/script.cpp</c>.
/// </summary>
public static class AgitEventNames
{
    public const string Start = "OnAgitStart";
    public const string End = "OnAgitEnd";
    public const string Init = "OnAgitInit";

    public const string Start2 = "OnAgitStart2";
    public const string End2 = "OnAgitEnd2";
    public const string Init2 = "OnAgitInit2";

    public const string Start3 = "OnAgitStart3";
    public const string End3 = "OnAgitEnd3";
    public const string Init3 = "OnAgitInit3";
}
