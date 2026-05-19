using Map.Server.Entities;

namespace Map.Server.Scripting.Vars;

/// <summary>
/// rAthena <c>pc_setreg</c> / <c>pc_readreg</c> / <c>pc_setregistry</c>
/// / <c>pc_readregistry</c> (pc.cpp:2840-3060) — script-variable
/// storage. Maps the rAthena prefix system:
///
/// <list type="bullet">
///   <item><c>@var</c> — temporary char-scope (lost on logout).</item>
///   <item><c>var</c> — permanent char-scope (<c>char_reg_num</c> /
///         <c>char_reg_str</c>).</item>
///   <item><c>#var</c> — permanent account-scope
///         (<c>acc_reg_num</c> / <c>acc_reg_str</c>).</item>
///   <item><c>##var</c> — permanent global-account-scope
///         (<c>global_acc_reg_num</c> / <c>global_acc_reg_str</c>).</item>
/// </list>
///
/// All persistent stores back through <see cref="VarScope"/>; the
/// service composites in-memory cache + DbContext flush so frequent
/// reads inside a script don't round-trip the DB.
/// </summary>
public interface IPlayerVarService
{
    /// <summary>Read a numeric variable. Returns 0 when unset (matching rAthena).</summary>
    long ReadNum(PlayerEntity pc, VarScope scope, string name, uint index = 0);

    /// <summary>Read a string variable. Returns empty when unset.</summary>
    string ReadStr(PlayerEntity pc, VarScope scope, string name, uint index = 0);

    /// <summary>Write a numeric variable. Value 0 deletes the row.</summary>
    void WriteNum(PlayerEntity pc, VarScope scope, string name, long value, uint index = 0);

    /// <summary>Write a string variable. Empty value deletes the row.</summary>
    void WriteStr(PlayerEntity pc, VarScope scope, string name, string value, uint index = 0);

    /// <summary>
    /// rAthena <c>pc_reg_received</c> — load every persistent var for
    /// the session into the in-memory cache. Called once at session enter.
    /// </summary>
    Task LoadAsync(PlayerEntity pc, CancellationToken ct);

    /// <summary>Flush dirty entries to the DB. Called from autosave + at logout.</summary>
    Task FlushAsync(PlayerEntity pc, CancellationToken ct);
}

/// <summary>
/// rAthena script-variable scope. Maps each entry to its persistence
/// table (or to the in-memory-only temporary scope).
/// </summary>
public enum VarScope
{
    /// <summary><c>@var</c> — temporary char-scope, in-memory only.</summary>
    CharTemp,
    /// <summary><c>var</c> — char_reg_num / char_reg_str.</summary>
    Char,
    /// <summary><c>#var</c> — acc_reg_num / acc_reg_str.</summary>
    Account,
    /// <summary><c>##var</c> — global_acc_reg_num / global_acc_reg_str.</summary>
    GlobalAccount,
}
