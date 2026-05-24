using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// Default <see cref="IPlayerSkillService"/>. Validates against the
/// loaded <see cref="ISkillDb"/>, tracks per-skill
/// <see cref="SkillFlag"/> so `pc_clean_skilltree` can drop only
/// tree-issued / temporary entries, and exposes the rAthena
/// plagiarism / job-tree-validation surface.
/// </summary>
public sealed class PlayerSkillService : IPlayerSkillService
{
    private readonly ISkillDb _db;
    private readonly ISkillTreeService? _skillTree;
    private readonly ILogger<PlayerSkillService> _logger;

    public PlayerSkillService(ISkillDb db, ILogger<PlayerSkillService> logger, ISkillTreeService? skillTree = null)
    {
        _db = db;
        _logger = logger;
        _skillTree = skillTree;
    }

    public bool Grant(PlayerEntity pc, ushort skillId, int level, GrantKind kind = GrantKind.Permanent)
    {
        var def = _db.Get(skillId);
        if (def == null)
        {
            _logger.LogWarning("pc_skill: unknown skill {Id}", skillId);
            return false;
        }

        var current = pc.LearnedSkills.GetValueOrDefault(skillId);
        var target = kind switch
        {
            GrantKind.TemporaryAdd => Math.Min(current + level, def.MaxLevel),
            _ => level,
        };
        target = Math.Clamp(target, 0, def.MaxLevel);

        if (target == 0)
        {
            pc.LearnedSkills.Remove(skillId);
            pc.SkillFlags.Remove(skillId);
            _logger.LogInformation("pc_skill: char {Char} lost skill {Id}", pc.CharacterId, skillId);
            return true;
        }
        pc.LearnedSkills[skillId] = (byte)target;
        pc.SkillFlags[skillId] = kind switch
        {
            GrantKind.Permanent => SkillFlag.Permanent,
            GrantKind.Temporary or GrantKind.TemporaryAdd => SkillFlag.Temporary,
            GrantKind.PermanentGranted => SkillFlag.PermanentGranted,
            _ => SkillFlag.Permanent,
        };
        _logger.LogInformation(
            "pc_skill: char {Char} learned skill {Id} lv {Lv} ({Kind})",
            pc.CharacterId, skillId, target, kind);
        return true;
    }

    public void Revoke(PlayerEntity pc, ushort skillId)
        => Grant(pc, skillId, 0, GrantKind.Permanent);

    public void CalcSkillTree(PlayerEntity pc)
    {
        // rAthena pc_calc_skilltree (pc.cpp:1611) validates each
        // LearnedSkills entry against the skill_db MaxLevel and
        // backfills any missing SkillFlag rows with Permanent.
        //
        // DBR-1f: ISkillTreeService is now wired; the per-job
        // override + inheritance walk + Exclude flag are queryable
        // via _skillTree.GetMaxLevel(jobAegis, skillAegis). Per-job
        // clamping isn't applied in this loop because PlayerEntity
        // doesn't carry a Class/JobAegis field yet (the rAthena
        // sd->status.class_ ↔ JobName table hasn't ported). When that
        // field lands, swap def.MaxLevel below for the per-job cap.
        foreach (var (id, lv) in pc.LearnedSkills.ToArray())
        {
            var def = _db.Get(id);
            if (def == null)
            {
                pc.LearnedSkills.Remove(id);
                pc.SkillFlags.Remove(id);
                continue;
            }
            if (lv > def.MaxLevel)
            {
                pc.LearnedSkills[id] = (byte)def.MaxLevel;
            }
            if (!pc.SkillFlags.ContainsKey(id))
            {
                pc.SkillFlags[id] = SkillFlag.Permanent;
            }
        }
    }

    /// <summary>
    /// DBR-1f: per-job-aware max-level resolver. When the caller knows
    /// the PC's job aegis (e.g. from a future <c>pc.JobAegis</c> field
    /// or a join service), this returns the effective cap walking the
    /// Inherit chain + honoring Exclude. Falls back to the global
    /// skill_db cap when the job tree doesn't list the skill.
    /// </summary>
    public int GetEffectiveMaxLevel(string jobAegis, ushort skillId)
    {
        var def = _db.Get(skillId);
        if (def == null) return 0;
        if (_skillTree == null || !_skillTree.HasData) return def.MaxLevel;
        var perJob = _skillTree.GetMaxLevel(jobAegis, def.Name);
        return perJob > 0 ? perJob : def.MaxLevel;
    }

    /// <summary>
    /// DBR-1f: per-job-aware prereq check. Walks the
    /// skill_tree_requirement_db chain via the inheritance graph.
    /// Returns true when no requirements exist (vacuous) or the
    /// service is offline.
    /// </summary>
    public bool CheckSkillRequirements(string jobAegis, ushort skillId, PlayerEntity pc)
    {
        if (_skillTree == null || !_skillTree.HasData) return true;
        var def = _db.Get(skillId);
        if (def == null) return false;
        // Project the PC's learned table from ushort-id to aegis-keyed
        // for the requirement walk (the catalog stores prereqs as
        // aegis strings to keep the join with skill_db cheap).
        var learnedByAegis = new Dictionary<string, int>(pc.LearnedSkills.Count, System.StringComparer.OrdinalIgnoreCase);
        foreach (var (id, lv) in pc.LearnedSkills)
        {
            var d = _db.Get(id);
            if (d != null && !string.IsNullOrEmpty(d.Name)) learnedByAegis[d.Name] = lv;
        }
        return _skillTree.CheckRequirements(jobAegis, def.Name, learnedByAegis);
    }

    public void CleanSkillTree(PlayerEntity pc)
    {
        // rAthena: drop PERMANENT_GRANTED + TEMPORARY (the player-paid
        // Permanent ones survive a job change). Used by pc_jobchange +
        // atcommand_lostskill.
        foreach (var (id, flag) in pc.SkillFlags.ToArray())
        {
            if (flag == SkillFlag.PermanentGranted || flag == SkillFlag.Temporary)
            {
                pc.LearnedSkills.Remove(id);
                pc.SkillFlags.Remove(id);
            }
        }
    }

    public bool TryPlagiarize(PlayerEntity pc, ushort skillId, ushort skillLevel)
    {
        var def = _db.Get(skillId);
        if (def == null) return false;
        if (skillLevel < 1 || skillLevel > def.MaxLevel) return false;
        // Drop any previously-plagiarized skill first (single slot).
        if (pc.PlagiarizedSkillId != 0)
        {
            pc.LearnedSkills.Remove(pc.PlagiarizedSkillId);
            pc.SkillFlags.Remove(pc.PlagiarizedSkillId);
        }
        pc.PlagiarizedSkillId = skillId;
        pc.PlagiarizedSkillLevel = (byte)skillLevel;
        pc.LearnedSkills[skillId] = (byte)skillLevel;
        pc.SkillFlags[skillId] = SkillFlag.Plagiarized;
        return true;
    }

    public void PlagiarismReset(PlayerEntity pc, byte type)
    {
        if (pc.PlagiarizedSkillId != 0)
        {
            pc.LearnedSkills.Remove(pc.PlagiarizedSkillId);
            pc.SkillFlags.Remove(pc.PlagiarizedSkillId);
        }
        pc.PlagiarizedSkillId = 0;
        pc.PlagiarizedSkillLevel = 0;
    }

    public bool Validate(PlayerEntity pc, ushort skillId, int level)
    {
        var def = _db.Get(skillId);
        if (def == null) return false;
        return level >= 0 && level <= def.MaxLevel;
    }

    public int CheckSkill(PlayerEntity? pc, ushort skillId)
        => pc is null ? 0 : pc.LearnedSkills.GetValueOrDefault(skillId);

    public int CheckImperialGuard(PlayerEntity pc, ushort skillId)
        => pc.LearnedSkills.GetValueOrDefault(skillId);

    public int CheckSummoner(PlayerEntity pc, ushort skillType)
        => pc.LearnedSkills.GetValueOrDefault(skillType);
}
