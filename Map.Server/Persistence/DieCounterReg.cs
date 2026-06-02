namespace Map.Server.Persistence;

/// <summary>
/// COMBAT-52 — read/write the <c>die_counter</c> as the per-character permanent
/// register <c>PC_DIE_COUNTER</c> (rAthena <c>PCDIECOUNTER_VAR</c>,
/// <c>pc_readglobalreg</c> / <c>pc_setglobalreg</c>). Persists through the existing
/// perm var-reg pipeline (<see cref="PlayerStateService"/> → <c>char_reg_num</c>),
/// so no schema column is needed — this matches how rAthena stores it.
/// </summary>
public static class DieCounterReg
{
    /// <summary>rAthena <c>PCDIECOUNTER_VAR</c>.</summary>
    public const string VarName = "PC_DIE_COUNTER";

    /// <summary>Read the persisted die_counter from the loaded perm scope (0 if absent).</summary>
    public static int Read(PlayerVarRegs? regs)
        => regs?.Perm.Bag is { } bag
           && bag.TryGetValue(VarName, out var v) && v != null
            ? (int)System.Convert.ToInt64(v)
            : 0;

    /// <summary>
    /// Mirror the live die_counter into the perm scope so the SavePerm diff writes it
    /// to <c>char_reg_num</c>. rAthena drops 0-value registers, so a 0 count is left
    /// absent (re-reads as 0).
    /// </summary>
    public static void Persist(PlayerVarRegs regs, int dieCounter)
    {
        if (dieCounter != 0) regs.Perm.Bag[VarName] = (long)dieCounter;
    }
}
