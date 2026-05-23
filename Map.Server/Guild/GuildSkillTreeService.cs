using Core.Database.Repositories.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Guild;

/// <summary>
/// Default <see cref="IGuildSkillTreeService"/>. Loads
/// guild_skill_tree_db (16 rows ish) + guild_skill_tree_requirement_db
/// (37 prereqs) at boot. Singleton with a one-shot scope.
/// </summary>
public sealed class GuildSkillTreeService : IGuildSkillTreeService
{
    private readonly Dictionary<string, ushort> _maxLevels = new(System.StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<(string Required, int Level)>> _reqs = new(System.StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<GuildSkillTreeService>? _logger;

    /// <summary>
    /// GD_* skill_id → aegis name. Mirrors the enum block in
    /// rAthena common/mmo.hpp (GD_APPROVAL = 10000 … GD_EMERGENCY_MOVE).
    /// Used to translate the numeric IDs the guild runtime carries to
    /// aegis-keyed catalog lookups.
    /// </summary>
    private static readonly System.Collections.Generic.Dictionary<ushort, string> AegisById = new()
    {
        { 10000, "GD_APPROVAL" },
        { 10001, "GD_KAFRACONTRACT" },
        { 10002, "GD_GUARDRESEARCH" },
        { 10003, "GD_GUARDUP" },
        { 10004, "GD_EXTENSION" },
        { 10005, "GD_GLORYGUILD" },
        { 10006, "GD_LEADERSHIP" },
        { 10007, "GD_GLORYWOUNDS" },
        { 10008, "GD_SOULCOLD" },
        { 10009, "GD_HAWKEYES" },
        { 10010, "GD_BATTLEORDER" },
        { 10011, "GD_REGENERATION" },
        { 10012, "GD_RESTORE" },
        { 10013, "GD_EMERGENCYCALL" },
        { 10014, "GD_DEVELOPMENT" },
        { 10015, "GD_ITEMEMERGENCYCALL" },
        { 10016, "GD_GUILD_STORAGE" },
        { 10017, "GD_CHARGESHOUT_FLAG" },
        { 10018, "GD_CHARGESHOUT_BEATING" },
        { 10019, "GD_EMERGENCY_MOVE" },
    };

    private static readonly System.Collections.Generic.Dictionary<string, ushort> IdByAegis =
        BuildReverseMap();
    private static System.Collections.Generic.Dictionary<string, ushort> BuildReverseMap()
    {
        var m = new System.Collections.Generic.Dictionary<string, ushort>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var (id, a) in AegisById) m[a] = id;
        return m;
    }

    public bool HasData => _maxLevels.Count > 0;

    public GuildSkillTreeService(IServiceScopeFactory scopes, ILogger<GuildSkillTreeService> logger)
    {
        _logger = logger;
        LoadFromDb(scopes);
    }

    /// <summary>Test ctor — leaves the cache empty.</summary>
    public GuildSkillTreeService() { }

    private void LoadFromDb(IServiceScopeFactory scopes)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IGuildSkillTreeDbRepository>();
            var rows = repo.GetAllAsync().GetAwaiter().GetResult();
            if (rows.Count == 0)
            {
                _logger?.LogInformation("guild_skill_tree_db is empty — guild skill caps disabled");
                return;
            }
            foreach (var row in rows)
            {
                _maxLevels[row.SkillAegis] = (ushort)System.Math.Max(0, row.MaxLevel);
                var reqs = repo.GetRequirementsAsync(row.SkillAegis).GetAwaiter().GetResult();
                if (reqs.Count == 0) continue;
                var list = new List<(string, int)>(reqs.Count);
                foreach (var r in reqs)
                {
                    list.Add((r.RequiredSkillAegis, r.RequiredLevel));
                }
                _reqs[row.SkillAegis] = list;
            }
            _logger?.LogInformation(
                "Loaded {N} guild skills from guild_skill_tree_db ({Reqs} prereq rows)",
                _maxLevels.Count, _reqs.Sum(kv => kv.Value.Count));
        }
        catch (System.Exception ex)
        {
            _logger?.LogWarning(ex, "guild_skill_tree_db load failed");
        }
    }

    public ushort GetMaxLevel(string skillAegis)
        => _maxLevels.TryGetValue(skillAegis ?? string.Empty, out var v) ? v : (ushort)0;

    public ushort GetMaxLevel(ushort skillId)
        => AegisById.TryGetValue(skillId, out var aegis) ? GetMaxLevel(aegis) : (ushort)0;

    public bool CheckRequirements(string skillAegis, System.Collections.Generic.IReadOnlyDictionary<string, int> learnedSkillsByAegis)
    {
        if (string.IsNullOrEmpty(skillAegis)) return true;
        if (!_reqs.TryGetValue(skillAegis, out var list)) return true;
        foreach (var (req, level) in list)
        {
            var have = learnedSkillsByAegis.TryGetValue(req, out var lv) ? lv : 0;
            if (have < level) return false;
        }
        return true;
    }

    public bool CheckRequirements(ushort skillId, System.Collections.Generic.IReadOnlyDictionary<ushort, int> learnedSkillsById)
    {
        if (!AegisById.TryGetValue(skillId, out var aegis)) return true;
        if (!_reqs.TryGetValue(aegis, out var list)) return true;
        foreach (var (req, level) in list)
        {
            // Translate aegis prereq back to numeric id; if unmapped,
            // we conservatively treat it as missing (returns false).
            if (!IdByAegis.TryGetValue(req, out var reqId)) return false;
            var have = learnedSkillsById.TryGetValue(reqId, out var lv) ? lv : 0;
            if (have < level) return false;
        }
        return true;
    }
}
