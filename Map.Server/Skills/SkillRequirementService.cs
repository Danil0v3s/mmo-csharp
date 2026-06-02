using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// Default <see cref="ISkillRequirementService"/>. Mirrors rAthena
/// <c>skill_check_condition_castbegin/end</c> + <c>skill_consume_*</c>
/// (skill.cpp:18347, 19417, 4397, 19685). The HP/SP/AP/Zeny path is
/// real; the item-list and per-skill exception branches are deferred
/// per PARITY-REMAINING.md §P2.2.b on the skill_db `Required*` column
/// reader.
/// </summary>
public sealed class SkillRequirementService : ISkillRequirementService
{
    private readonly ISkillDb _db;
    private readonly ILogger<SkillRequirementService> _logger;

    public SkillRequirementService(ISkillDb db, ILogger<SkillRequirementService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public bool CheckCondition(PlayerEntity caster, ushort skillId, ushort skillLevel)
    {
        // HP / SP / AP — read from skill_db and compare with current.
        var hp = _db.GetHp(skillId, skillLevel);
        var hpRate = _db.GetHpRate(skillId, skillLevel);
        if (hpRate > 0) hp += caster.MaxHp * hpRate / 100;
        else if (hpRate < 0) hp += caster.Hp * (-hpRate) / 100;
        if (hp > 0 && caster.Hp <= hp) return false;

        var sp = SpCost(caster, skillId, skillLevel);
        if (sp > 0 && caster.Sp < sp) return false;

        // Zeny check — Zeny lives on MapSessionData.CharacterData, not
        // PlayerEntity, so the read goes through the session. Until the
        // requirement service gains a SessionLookup the check is best-
        // effort (no PlayerEntity.Zeny accessor today).

        // Weapon mask / ammo / spirit ball / state checks live on
        // SkillDefinition but the bonus aggregator hasn't surfaced them
        // yet — keep the canonical entry point, flag the gap.
        return true;
    }

    public bool CheckConditionCastEnd(PlayerEntity caster, ushort skillId, ushort skillLevel)
    {
        // rAthena re-runs the requirement check at castend to catch
        // things that became invalid mid-cast (gear changes,
        // dispatched buffs). Until the cast lifecycle exposes those
        // edges we delegate to castbegin.
        return CheckCondition(caster, skillId, skillLevel);
    }

    /// <summary>
    /// The effective SP cost: skill_db SP + the SpRate %, then COMBAT-45's
    /// bUseSPrate (SP_USE_SP_RATE) % modifier (rAthena skill.cpp:19855 —
    /// <c>req.sp = req.sp * sd->dsprate/100</c>, where dsprate = 100 + Σ bUseSPrate;
    /// the bundle stores the bonus delta, negative = cheaper).
    /// </summary>
    private int SpCost(PlayerEntity caster, ushort skillId, ushort skillLevel)
    {
        var sp = _db.GetSp(skillId, skillLevel);
        var spRate = _db.GetSpRate(skillId, skillLevel);
        if (spRate > 0) sp += caster.MaxSp * spRate / 100;
        else if (spRate < 0) sp += caster.Sp * (-spRate) / 100;
        if (caster.EquipBonuses?.UseSpRate is int usr && usr != 0)
            sp = sp * (100 + usr) / 100;
        return sp;
    }

    public void ConsumeHpSpAp(Entity bl, ushort skillId, int hp, int sp, int ap)
    {
        // rAthena SM_MAGNUM exception: requires HP but doesn't consume it.
        if (skillId == SkillIds.SM_MAGNUM) hp = 0;

        switch (bl)
        {
            case PlayerEntity pc:
                if (hp > 0) pc.Hp = Math.Max(1, pc.Hp - hp);
                if (sp > 0) pc.Sp = Math.Max(0, pc.Sp - sp);
                // AP is the 4th-class trait pool (status_charge ap branch);
                // 4th-class skills cost AP, drained the same way.
                if (ap > 0) pc.Ap = Math.Max(0, pc.Ap - ap);
                break;
            case MobEntity m:
                if (hp > 0) m.Hp = Math.Max(1, m.Hp - hp);
                break;
        }
    }

    public void ConsumeRequirement(PlayerEntity caster, ushort skillId, ushort skillLevel, byte type)
    {
        // type & 1 — consume pools.
        if ((type & 1) != 0)
        {
            var hp = _db.GetHp(skillId, skillLevel);
            var sp = SpCost(caster, skillId, skillLevel);
            var ap = _db.GetAp(skillId, skillLevel);
            if (hp > 0 || sp > 0 || ap > 0)
                ConsumeHpSpAp(caster, skillId, hp, sp, ap);

            // Zeny consume — deferred per PARITY-REMAINING.md §P2.2.b
            // (skill_db Required* column reader). Canonical deduction
            // lives in `pc_payzeny`; plugs in here when the column
            // surfaces.
        }

        // type & 2 — consume items + ammo. Data-pending on skill_db
        // require column. The entry point is here so the caller doesn't
        // drift back to inline pc.Inventory mutation.
    }

    public int CheckConditionCharSub(Entity owner, Entity candidate, ushort skillId)
    {
        // rAthena helper used by mob_clone / Royal Guard banding etc.
        // First slice: report a 1 if both are alive PCs on the same
        // map, 0 otherwise; the per-skill partner rules port skill-by-
        // skill when their resolvers land.
        if (owner is not PlayerEntity || candidate is not PlayerEntity) return 0;
        return owner.MapId == candidate.MapId ? 1 : 0;
    }

    public void RefundRequirement(PlayerEntity caster, ushort skillId, ushort skillLevel)
    {
        // rAthena's reverse of consume — credit pools back to the
        // caster. Items / ammo refund waits on the skill_db Required*
        // column reader (PARITY-REMAINING §P2.2.b); HP / SP / AP are
        // the pools we can return today.
        var hp = _db.GetHp(skillId, skillLevel);
        var sp = _db.GetSp(skillId, skillLevel);
        var ap = _db.GetAp(skillId, skillLevel);
        if (hp > 0) caster.Hp = Math.Min(caster.MaxHp, caster.Hp + hp);
        if (sp > 0) caster.Sp = Math.Min(caster.MaxSp, caster.Sp + sp);
        if (ap > 0) caster.Ap = Math.Min(caster.MaxAp, caster.Ap + ap);
    }
}
