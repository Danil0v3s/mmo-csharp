using Core.Database.Repositories.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// Default <see cref="ISkillTreeService"/>. Snapshots the four
/// skill_tree_* tables at boot (skill_tree_db parent, _inherit, _entry,
/// _requirement) into per-job <see cref="JobSkillTree"/> records.
/// Static catalog → singleton with a one-shot scope at construction.
///
/// DBR-1f: replaces the in-process "skill_db.MaxLevel only" path. The
/// per-job override matters because rAthena's skill_db carries the
/// *global* MaxLevel; per-job rows often cap lower (or set zero =
/// not-learnable for that class).
/// </summary>
public sealed class SkillTreeService : ISkillTreeService
{
    private readonly ILogger<SkillTreeService>? _logger;

    /// <summary>job_aegis → resolved tree node.</summary>
    private readonly Dictionary<string, JobSkillTree> _byJob = new(System.StringComparer.OrdinalIgnoreCase);

    public bool HasData => _byJob.Count > 0;

    public SkillTreeService(IServiceScopeFactory scopes, ILogger<SkillTreeService> logger)
    {
        _logger = logger;
        LoadFromDb(scopes);
    }

    /// <summary>Test ctor — leaves the cache empty.</summary>
    public SkillTreeService() { }

    private void LoadFromDb(IServiceScopeFactory scopes)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ISkillTreeDbRepository>();
            var parents = repo.GetAllAsync().GetAwaiter().GetResult();
            if (parents.Count == 0)
            {
                _logger?.LogInformation("skill_tree_db is empty — per-job overrides disabled");
                return;
            }

            // Bulk-fetch every entry/inherit/requirement once, then bucket
            // by JobAegis. Avoids 175 N+1 round-trips at boot.
            var allEntries = new Dictionary<string, List<(string SkillAegis, int MaxLevel, bool Exclude)>>(System.StringComparer.OrdinalIgnoreCase);
            var allInherits = new Dictionary<string, List<string>>(System.StringComparer.OrdinalIgnoreCase);
            var allReqs = new Dictionary<(string Job, string Skill), List<(string Required, int Level)>>();

            foreach (var p in parents)
            {
                allEntries[p.JobAegis] = new List<(string, int, bool)>();
                allInherits[p.JobAegis] = new List<string>();
            }

            // Walk entries + reqs per parent. The per-parent calls are
            // batched per-job (~175 jobs × ~10ms each = ~2s worst case),
            // which is acceptable boot-time. A future GetAllEntriesAsync
            // / GetAllReqsAsync could collapse to one round-trip apiece.
            foreach (var p in parents)
            {
                var entries = repo.GetEntriesAsync(p.JobAegis).GetAwaiter().GetResult();
                foreach (var e in entries)
                {
                    allEntries[p.JobAegis].Add((e.SkillAegis, e.MaxLevel, e.Exclude));
                }

                var inherits = repo.GetInheritsAsync(p.JobAegis).GetAwaiter().GetResult();
                foreach (var i in inherits)
                {
                    allInherits[p.JobAegis].Add(i.ParentJobAegis);
                }

                foreach (var e in entries)
                {
                    var reqs = repo.GetRequirementsAsync(p.JobAegis, e.SkillAegis).GetAwaiter().GetResult();
                    if (reqs.Count == 0) continue;
                    var key = (p.JobAegis, e.SkillAegis);
                    if (!allReqs.TryGetValue(key, out var list))
                    {
                        list = new List<(string, int)>();
                        allReqs[key] = list;
                    }
                    foreach (var r in reqs)
                    {
                        list.Add((r.RequiredSkillAegis, r.RequiredLevel));
                    }
                }
            }

            foreach (var p in parents)
            {
                var entriesByAegis = new Dictionary<string, (int MaxLevel, bool Exclude)>(System.StringComparer.OrdinalIgnoreCase);
                foreach (var e in allEntries[p.JobAegis])
                {
                    entriesByAegis[e.SkillAegis] = (e.MaxLevel, e.Exclude);
                }
                var reqsForJob = new Dictionary<string, List<(string Required, int Level)>>(System.StringComparer.OrdinalIgnoreCase);
                foreach (var kv in allReqs)
                {
                    if (!string.Equals(kv.Key.Job, p.JobAegis, System.StringComparison.OrdinalIgnoreCase)) continue;
                    reqsForJob[kv.Key.Skill] = kv.Value;
                }
                _byJob[p.JobAegis] = new JobSkillTree(
                    p.JobAegis,
                    allInherits[p.JobAegis].ToArray(),
                    entriesByAegis,
                    reqsForJob);
            }
            _logger?.LogInformation(
                "Loaded {Jobs} jobs from skill_tree_db ({Entries} entries, {Inherits} inherit edges)",
                _byJob.Count,
                allEntries.Sum(kv => kv.Value.Count),
                allInherits.Sum(kv => kv.Value.Count));
        }
        catch (System.Exception ex)
        {
            _logger?.LogWarning(ex, "skill_tree_db load failed — falling back to global skill_db MaxLevel");
        }
    }

    public int GetMaxLevel(string jobAegis, string skillAegis)
    {
        if (string.IsNullOrEmpty(jobAegis) || string.IsNullOrEmpty(skillAegis)) return 0;
        // Walk job + Inherit chain. If a child marks an entry Exclude=true
        // it means that entry is *the child's own private skill, not
        // inheritable* — so the recursion must NOT bubble it down further.
        // From the perspective of a Knight asking "do I get NV_TRICKDEAD?",
        // walking Novice will find TRICKDEAD with Exclude=true, but
        // Exclude means "Novice keeps this, children don't" — so the
        // Knight should NOT see TRICKDEAD inherited. Same flag, two
        // directions: when querying *for the parent itself*, Exclude is
        // ignored; when querying *via inheritance*, Exclude blocks.
        return WalkForMax(jobAegis, skillAegis, viaInherit: false, visited: new HashSet<string>(System.StringComparer.OrdinalIgnoreCase));
    }

    private int WalkForMax(string jobAegis, string skillAegis, bool viaInherit, HashSet<string> visited)
    {
        if (!visited.Add(jobAegis)) return 0; // cycle guard
        if (!_byJob.TryGetValue(jobAegis, out var tree)) return 0;

        if (tree.Entries.TryGetValue(skillAegis, out var e))
        {
            // Excluded entries are private to the owning job — they show
            // up when querying that job directly, but not via inheritance.
            if (viaInherit && e.Exclude) { /* skip & try parents */ }
            else return e.MaxLevel;
        }

        // Walk parents.
        foreach (var parent in tree.Parents)
        {
            var inherited = WalkForMax(parent, skillAegis, viaInherit: true, visited);
            if (inherited > 0) return inherited;
        }
        return 0;
    }

    public bool IsLearnable(string jobAegis, string skillAegis)
        => GetMaxLevel(jobAegis, skillAegis) > 0;

    public bool CheckRequirements(string jobAegis, string skillAegis, System.Collections.Generic.IReadOnlyDictionary<string, int> learnedSkillsByAegis)
    {
        if (string.IsNullOrEmpty(jobAegis) || string.IsNullOrEmpty(skillAegis)) return true;
        // Locate the row that *owns* the prereq table. Same Exclude
        // semantics as MaxLevel — when walking inheritance, an Exclude
        // entry is the parent's private business and shouldn't apply
        // to the child. In practice, prereqs sit on the same row as
        // the entry, so we walk together.
        var reqs = ResolveRequirements(jobAegis, skillAegis, viaInherit: false, visited: new HashSet<string>(System.StringComparer.OrdinalIgnoreCase));
        if (reqs == null || reqs.Count == 0) return true;
        foreach (var (req, level) in reqs)
        {
            var have = learnedSkillsByAegis.TryGetValue(req, out var lv) ? lv : 0;
            if (have < level) return false;
        }
        return true;
    }

    private IReadOnlyList<(string Required, int Level)>? ResolveRequirements(
        string jobAegis, string skillAegis, bool viaInherit, HashSet<string> visited)
    {
        if (!visited.Add(jobAegis)) return null;
        if (!_byJob.TryGetValue(jobAegis, out var tree)) return null;

        if (tree.Entries.TryGetValue(skillAegis, out var e))
        {
            if (viaInherit && e.Exclude) { /* skip */ }
            else
            {
                return tree.Requirements.TryGetValue(skillAegis, out var rs)
                    ? rs
                    : System.Array.Empty<(string, int)>();
            }
        }

        foreach (var parent in tree.Parents)
        {
            var inherited = ResolveRequirements(parent, skillAegis, viaInherit: true, visited);
            if (inherited != null) return inherited;
        }
        return null;
    }

    /// <summary>One job's resolved skill tree slice from the DB.</summary>
    public sealed record JobSkillTree(
        string JobAegis,
        string[] Parents,
        IReadOnlyDictionary<string, (int MaxLevel, bool Exclude)> Entries,
        IReadOnlyDictionary<string, List<(string Required, int Level)>> Requirements);
}
