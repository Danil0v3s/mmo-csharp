using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Map.Server.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Achievement;

/// <summary>
/// Default <see cref="IAchievementService"/>. Catalog loaded from
/// <c>achievement_db</c> (seeded from
/// <c>db/re/achievement_db.yml</c>, ~362 rows). Per-character
/// progress lives on the achievement table accessed via IPC.
/// </summary>
public sealed class AchievementService : IAchievementService
{
    private readonly Dictionary<uint, AchievementDbEntity> _catalog = new();
    private readonly IServiceScopeFactory? _scopes;
    private readonly ILogger<AchievementService> _logger;

    public AchievementService(IServiceScopeFactory scopes, ILogger<AchievementService> logger)
    {
        _scopes = scopes;
        _logger = logger;
        ReloadDb();
    }

    public AchievementService(ILogger<AchievementService> logger) { _logger = logger; }

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
    public int Level(PlayerEntity pc) => 0;
    public bool MobExists(int mobId) => false;

    public void ReloadDb()
    {
        _catalog.Clear();
        if (_scopes == null) return;
        try
        {
            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAchievementDbRepository>();
            foreach (var a in repo.GetAllAsync().GetAwaiter().GetResult())
                _catalog[a.AchievementId] = a;
            _logger.LogInformation("achievement_db loaded: {N} achievements", _catalog.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "achievement_db load failed");
        }
    }

    /// <summary>Catalog lookup — null if unknown.</summary>
    public AchievementDbEntity? GetCatalogEntry(uint achievementId)
        => _catalog.TryGetValue(achievementId, out var v) ? v : null;
}
