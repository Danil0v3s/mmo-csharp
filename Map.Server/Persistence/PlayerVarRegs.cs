namespace Map.Server.Persistence;

/// <summary>
/// Bundle of the three persistent variable scopes loaded for one player.
/// Lives on <see cref="Session.MapSessionData.VarRegs"/> while the player
/// is on the map.
/// </summary>
public sealed class PlayerVarRegs
{
    /// <summary>rAthena's <c>var</c> (bare prefix) — per-character permanent.</summary>
    public PlayerVarScope Perm { get; }

    /// <summary>rAthena's <c>#var</c> — per-account, local to this login server.</summary>
    public PlayerVarScope Account { get; }

    /// <summary>rAthena's <c>##var</c> — per-account, global across login servers.</summary>
    public PlayerVarScope AccountGlobal { get; }

    public PlayerVarRegs(PlayerVarScope perm, PlayerVarScope account, PlayerVarScope accountGlobal)
    {
        Perm = perm;
        Account = account;
        AccountGlobal = accountGlobal;
    }

    public static PlayerVarRegs Empty() => new(
        new PlayerVarScope(new Dictionary<string, object>()),
        new PlayerVarScope(new Dictionary<string, object>()),
        new PlayerVarScope(new Dictionary<string, object>()));
}
