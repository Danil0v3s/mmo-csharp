namespace Map.Server.Status;

/// <summary>
/// Stat-point cost formulas for raising base stats. Mirrors rAthena
/// pc.cpp <c>PC_STATUS_POINT_COST</c> macro (renewal vs pre-renewal
/// branch — we only port the renewal one):
///
/// <code>
/// cost(low) = low &lt; 100 ? (2 + (low - 1) / 10)
///                       : (16 + 4 * ((low - 100) / 5))
/// </code>
///
/// The pre-renewal cost (<c>1 + ((low + 9) / 10)</c>) is permanently
/// out of scope (CLAUDE.md).
/// </summary>
public static class StatPointTable
{
    /// <summary>Cost (in status points) to raise a stat from <paramref name="current"/> to current+1.</summary>
    public static int CostToIncrement(int current)
    {
        if (current < 100) return 2 + (current - 1) / 10;
        return 16 + 4 * ((current - 100) / 5);
    }

    /// <summary>Sum of costs to raise from <paramref name="current"/> by <paramref name="delta"/> points.</summary>
    public static int TotalCost(int current, int delta)
    {
        if (delta <= 0) return 0;
        int sum = 0;
        for (int i = 0; i < delta; i++)
            sum += CostToIncrement(current + i);
        return sum;
    }

    /// <summary>Cap for base stats (rAthena pc_maxparameter default = 99).</summary>
    public const int MaxBaseStat = 99;
}
