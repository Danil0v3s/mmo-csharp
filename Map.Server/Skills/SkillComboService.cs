using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// Default <see cref="ISkillComboService"/>. Stores in-flight combo
/// state in a per-caster map; partner / banding helpers iterate
/// nearby PCs. Combo data — the per-skill follow-up list — is
/// data-pending on skill_db's `Combo: ...` column.
/// </summary>
public sealed class SkillComboService : ISkillComboService
{
    private readonly Dictionary<EntityId, (ushort skill, ushort level, long expiresAt)> _comboState = new();
    private readonly IEntityRegistry _entities;
    private readonly ISkillDb? _skillDb;
    private readonly ILogger<SkillComboService> _logger;

    public SkillComboService(IEntityRegistry entities, ILogger<SkillComboService> logger, ISkillDb? skillDb = null)
    {
        _entities = entities;
        _logger = logger;
        _skillDb = skillDb;
    }

    public void Combo(PlayerEntity caster, ushort skillId, ushort skillLevel, int durationMs)
    {
        _comboState[caster.Id] = (skillId, skillLevel, Environment.TickCount64 + Math.Max(0, durationMs));
    }

    public bool IsCombo(PlayerEntity caster, ushort skillId)
    {
        if (!_comboState.TryGetValue(caster.Id, out var s)) return false;
        if (s.expiresAt < Environment.TickCount64) { _comboState.Remove(caster.Id); return false; }
        // SK.100-1a — consult per-skill Combo chain. If the in-flight
        // combo's "next allowed" list (from skill_db) contains the
        // requested skill, the chain is alive. Falls back to the
        // legacy same-skill check when no SkillDb is wired.
        if (_skillDb != null)
        {
            var chain = _skillDb.GetCombo(s.skill);
            foreach (var nextId in chain)
                if (nextId == skillId) return true;
        }
        return s.skill == skillId;
    }

    public void ComboToggleInf(PlayerEntity caster, ushort skillId, bool combatTargeting)
    {
        // rAthena flips e_inf for combo skills (e.g. Triple Attack
        // becomes self-targeted between hits). The C# resolver
        // dispatch doesn't expose that knob today; reserved here.
    }

    public int CheckPcPartner(PlayerEntity caster, ushort skillId, ushort skillLevel, short range, bool consumeOnHit)
    {
        var nearby = _entities.ForEachInRange(caster.MapId, caster.X, caster.Y, range, EntityType.Pc);
        var count = 0;
        foreach (var e in nearby)
        {
            if (e.Id == caster.Id) continue;
            if (e is PlayerEntity other && IsAllied(caster, other)) count++;
        }
        return count;
    }

    public int BandingCount(PlayerEntity caster)
    {
        // Royal Guard Banding pulses a 7-cell range looking for partners
        // with SC_BANDING active. Until SC_BANDING ports we treat all
        // nearby party members within 7 cells as banding partners.
        return CheckPcPartner(caster, 0, 0, 7, consumeOnHit: false);
    }

    private static bool IsAllied(PlayerEntity a, PlayerEntity b)
    {
        if (a.PartyId != 0 && a.PartyId == b.PartyId) return true;
        if (a.GuildId != 0 && a.GuildId == b.GuildId) return true;
        return false;
    }
}
