namespace Map.Server.Achievement;

/// <summary>
/// Per-PC achievement progress entry. Runtime mirror of rAthena
/// <c>struct achievement</c> in <c>achievement.hpp</c>:
/// <code>
/// struct achievement {
///     int achievement_id;
///     int count[MAX_ACHIEVEMENT_OBJECTIVES];
///     time_t completed;
///     time_t rewarded;
///     int score;
/// };
/// </code>
/// Lives on <see cref="Map.Server.Entities.PlayerEntity.AchievementLog"/>.
/// T7.1 added it so <see cref="IAchievementService.SnapshotFor"/>
/// can serialize a real payload onto the <c>AchievementSaveAsync</c>
/// RPC.
/// </summary>
public sealed class AchievementEntry
{
    /// <summary>achievement_db row id.</summary>
    public int AchievementId { get; set; }

    /// <summary>Per-objective counters.</summary>
    public int[] Counts { get; set; } = System.Array.Empty<int>();

    /// <summary>Unix timestamp at which all objectives finished (0 = pending).</summary>
    public long CompletedUnix { get; set; }

    /// <summary>Unix timestamp at which the reward was claimed (0 = unclaimed).</summary>
    public long RewardedUnix { get; set; }

    /// <summary>Cumulative achievement-score contribution.</summary>
    public int Score { get; set; }
}
