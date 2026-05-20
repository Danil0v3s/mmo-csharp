using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills;

/// <summary>
/// Default <see cref="ISkillCastTimingService"/>. Mirrors rAthena
/// <c>skill_castfix</c> / <c>skill_castfix_sc</c> / <c>skill_vfcastfix</c>
/// / <c>skill_delayfix</c> (skill.cpp:20193 — 20565).
///
/// Item / card bonus inputs (<c>sd-&gt;bonus.add_varcast</c>,
/// <c>sd-&gt;bonus.varcastrate</c>, <c>sd-&gt;skillcastrate</c>, …)
/// are documented as data-pending — they flow through when the
/// equip-bonus aggregator surfaces them on
/// <see cref="PlayerEntity"/>. The DEX/AGI / config-rate paths are
/// real today.
/// </summary>
public sealed class SkillCastTimingService : ISkillCastTimingService
{
    private readonly ISkillDb _db;
    private readonly IBattleConfigService _cfg;

    public SkillCastTimingService(ISkillDb db, IBattleConfigService cfg)
    {
        _db = db;
        _cfg = cfg;
    }

    // ----- skill_castfix (pre-renewal) -------------------------------
    public int CastFix(Entity caster, ushort skillId, ushort skillLevel)
    {
        double time = _db.GetCast(skillId, skillLevel);
        if (time <= 0) return 0;

        var flag = _db.GetCastNoDex(skillId);

        // (flag & 1) == DEX cast-time bypass.
        if ((flag & 1) == 0)
        {
            var scale = _cfg.GetValue("castrate_dex_scale") - caster.Stats.Dex;
            if (scale > 0)
                time = time * scale / _cfg.GetValue("castrate_dex_scale");
            else
                return 0; // instant cast (DEX >= scale).
        }

        // Item / card bonuses are data-pending on PlayerEntity.
        // When the equip aggregator surfaces VariableCastrate / add_varcast
        // / skillcastrate they layer here.

        // Battle-config global rate.
        var rate = _cfg.GetValue("cast_rate");
        if (rate != 100) time = time * rate / 100;
        return Math.Max(0, (int)time);
    }

    // ----- skill_castfix_sc (pre-renewal SC overlay) -----------------
    public int CastFixSc(Entity caster, int time, byte flag = 0)
    {
        if (time < 0) return 0;
        if (caster is MobEntity or NpcEntity) return time;

        // SC overlay path — Suffragium, Memorize, Slowcast, Paralysis,
        // Izayoi. Data-pending on the SC table; we keep the canonical
        // entry point so callers don't have to inline the call.
        return Math.Max(0, time);
    }

    // ----- skill_vfcastfix (renewal variable + fixed) ----------------
    public int VfCastFix(Entity caster, int variableTime, ushort skillId, ushort skillLevel)
    {
        if (variableTime < 0) return 0;
        if (caster is MobEntity or NpcEntity) return variableTime;

        var fixedTime = _db.GetFixedCast(skillId, skillLevel);
        if (fixedTime < 0)
        {
            var def = _cfg.GetValue("default_fixed_castrate");
            if (def > 0)
            {
                fixedTime = variableTime * def / 100;
                variableTime = variableTime * (100 - def) / 100;
            }
            else
            {
                fixedTime = 0;
            }
        }

        // sd-&gt;bonus.add_varcast / add_fixcast / varcastrate / fixcastrate
        // and the per-skill skillvarcast/skillfixcast/skillcastrate tables
        // are data-pending on the equip-bonus aggregator.

        return Math.Max(0, variableTime + fixedTime);
    }

    // ----- skill_delayfix (after-cast delay) -------------------------
    public int DelayFix(Entity caster, ushort skillId, ushort skillLevel)
    {
        // SA_ABRACADABRA explicitly returns 0 — handled by data-pending
        // gate once the dummy-skill id table imports.

        // BL-type no-skill-delay bypass — rAthena returns the floor immediately.
        if (caster is MobEntity)
        {
            var no = _cfg.GetValue("no_skill_delay");
            if ((no & 2) != 0) return _cfg.GetValue("min_skill_delay_limit");
        }

        var delaynodex = _db.GetDelayNoDex(skillId);
        double time = _db.GetDelay(skillId, skillLevel);

        // rAthena: negative delay means "add to AMOTION" (attack motion).
        if (time < 0) time = -time + caster.Stats.Amotion;

        if (_cfg.GetValue("delay_dependon_dex") != 0 && (delaynodex & 1) == 0)
        {
            var scale = _cfg.GetValue("castrate_dex_scale") - caster.Stats.Dex;
            time = scale > 0 ? time * scale / _cfg.GetValue("castrate_dex_scale") : 0;
        }
        if (_cfg.GetValue("delay_dependon_agi") != 0 && (delaynodex & 1) == 0)
        {
            var scale = _cfg.GetValue("castrate_dex_scale") - caster.Stats.Agi;
            time = scale > 0 ? time * scale / _cfg.GetValue("castrate_dex_scale") : 0;
        }

        var rate = _cfg.GetValue("delay_rate");
        if (rate != 100) time = time * rate / 100;
        return Math.Max(0, (int)time);
    }
}
