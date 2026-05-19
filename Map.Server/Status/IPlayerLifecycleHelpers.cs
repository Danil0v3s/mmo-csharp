using Map.Server.Entities;

namespace Map.Server.Status;

/// <summary>
/// Remaining lifecycle helpers from pc.cpp:
/// <list type="bullet">
///   <item><c>pc_reg_received</c> (pc.cpp:1437) — fires once script
///         variables finish loading at session enter. Triggers
///         any "deferred until vars" state (achievement init,
///         attendance check, dialog reopen).</item>
///   <item><c>pc_makesavestatus</c> (pc.cpp:1325) — snapshot the PC
///         into a CharacterData payload for char-server autosave.</item>
///   <item><c>pc_setrestartvalue</c> (pc.cpp:1187) — set HP/SP to
///         their post-respawn values per battle.respawn_hp_rate.</item>
/// </list>
/// </summary>
public interface IPlayerLifecycleHelpers
{
    void OnRegReceived(PlayerEntity pc);
    void MakeSaveStatus(PlayerEntity pc);
    void SetRestartValue(PlayerEntity pc, byte type);
}

/// <summary>
/// Stat helpers — script-side bridges (<c>pc_setparam</c> /
/// <c>pc_readparam</c>), trait stats, parameter caps, cash payment.
/// </summary>
public interface IPlayerStatHelpers
{
    /// <summary>
    /// rAthena <c>pc_setparam</c> (pc.cpp:4828) — set SP_* by enum.
    /// Caller passes the numeric SP id (see SpId constants).
    /// </summary>
    bool SetParam(PlayerEntity pc, ushort spId, long value);

    /// <summary>rAthena <c>pc_readparam</c> — read SP_* by enum.</summary>
    long ReadParam(PlayerEntity pc, ushort spId);

    /// <summary>rAthena <c>pc_traitstatusup</c> (pc.cpp:4998) — renewal POW/STA/etc. allocation.</summary>
    bool TraitStatusUp(PlayerEntity pc, ushort traitId);

    /// <summary>
    /// rAthena <c>pc_maxparameter</c> (pc.cpp:5060) — class-cap
    /// lookup for a stat. First slice returns the global cap
    /// (rAthena <c>battle_config.max_parameter</c> default 99).
    /// </summary>
    int MaxParameter(PlayerEntity pc, ushort spId);

    /// <summary>rAthena <c>pc_maxbaselv</c> / <c>pc_maxjoblv</c> — class-aware level caps.</summary>
    int MaxBaseLevel(PlayerEntity pc);
    int MaxJobLevel(PlayerEntity pc);

    /// <summary>
    /// rAthena <c>pc_paycash</c> (pc.cpp:5475) — pay cash points or
    /// kafra points. <paramref name="pointsKafra"/> is consumed first,
    /// remainder from cash points. Returns false on insufficient funds.
    /// </summary>
    bool PayCash(PlayerEntity pc, int price, int pointsKafra);
}
