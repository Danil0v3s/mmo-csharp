namespace Map.Server.Persistence;

/// <summary>
/// GP-CASHSHOP — read/write the player's cash-shop currency as the per-account registers
/// <c>#CASHPOINTS</c> / <c>#KAFRAPOINTS</c> (rAthena <c>CASHPOINT_VAR</c> / <c>KAFRAPOINT_VAR</c>,
/// <c>pc_readaccountreg</c> / <c>pc_setaccountreg</c> — pc.cpp:2304/5766/5811). Account-bound, so a
/// player's balance is shared across their characters and survives logout. Persists through the
/// existing account var-reg pipeline (<see cref="PlayerStateService"/> → <c>acc_reg_num</c>), so no
/// schema column / proto field is needed — this is exactly how rAthena stores it.
/// </summary>
public static class CashPointsReg
{
    /// <summary>rAthena <c>CASHPOINT_VAR</c>.</summary>
    public const string CashVar = "#CASHPOINTS";

    /// <summary>rAthena <c>KAFRAPOINT_VAR</c>.</summary>
    public const string KafraVar = "#KAFRAPOINTS";

    /// <summary>Read the persisted cash points from the loaded account scope (0 if absent).</summary>
    public static int ReadCash(PlayerVarRegs? regs) => Read(regs, CashVar);

    /// <summary>Read the persisted kafra points from the loaded account scope (0 if absent).</summary>
    public static int ReadKafra(PlayerVarRegs? regs) => Read(regs, KafraVar);

    private static int Read(PlayerVarRegs? regs, string key)
        => regs?.Account.Bag is { } bag && bag.TryGetValue(key, out var v) && v != null
            ? (int)System.Convert.ToInt64(v)
            : 0;

    /// <summary>
    /// Mirror the live cash/kafra balances into the account scope so the SaveAccount diff writes them
    /// to <c>acc_reg_num</c>. A balance is always written when its register was already loaded (so
    /// spending down to 0 persists the 0); a brand-new 0 balance is left absent (rAthena drops 0-value
    /// registers — re-reads as 0).
    /// </summary>
    public static void Persist(PlayerVarRegs regs, int cashPoints, int kafraPoints)
    {
        Set(regs, CashVar, cashPoints);
        Set(regs, KafraVar, kafraPoints);
    }

    private static void Set(PlayerVarRegs regs, string key, int value)
    {
        if (value != 0 || regs.Account.Bag.ContainsKey(key))
            regs.Account.Bag[key] = (long)value;
    }
}
