using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Achievement;

/// <summary>Default <see cref="IAchievementService"/>. achievement_db YAML loader data-pending.</summary>
public sealed class AchievementService : IAchievementService
{
    private readonly ILogger<AchievementService> _logger;
    public AchievementService(ILogger<AchievementService> logger) => _logger = logger;

    public bool CheckCondition(PlayerEntity pc, int achievementId) => false;
    public bool CheckDependent(PlayerEntity pc, int achievementId) => false;
    public bool Remove(PlayerEntity pc, int achievementId) => false;
    public bool UpdateAchievement(PlayerEntity pc, int achievementId, bool completed) => false;
    public int CheckProgress(PlayerEntity pc, int achievementId) => 0;
    public int UpdateObjectiveSub(PlayerEntity pc, int achievementId, byte objective, int delta) => 0;
    public void UpdateObjective(PlayerEntity pc, byte type, byte index, int value) { }
    public void CheckReward(PlayerEntity pc, int achievementId) { }
    public void GetReward(PlayerEntity pc, int achievementId) { }
    public IReadOnlyList<int> GetTitles(PlayerEntity pc) => Array.Empty<int>();
    public void Free(PlayerEntity pc) { }
    public void ReloadDb() { }
    public int Level(PlayerEntity pc) => 0;
    public bool MobExists(int mobId) => false;
}
