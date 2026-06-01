using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Status;

namespace Map.Server.Skills;

/// <summary>
/// Default <see cref="ISkillCastTimingService"/>. Mirrors rAthena
/// <c>skill_castfix</c> / <c>skill_castfix_sc</c> / <c>skill_vfcastfix</c>
/// / <c>skill_delayfix</c> (skill.cpp:20193 — 20565).
///
/// COMBAT-07 wired the renewal DEX/INT variable-cast sqrt reduction and the
/// global equip/card cast bonuses (<c>varcastrate</c>/<c>fixcastrate</c>/
/// <c>add_varcast</c>/<c>add_fixcast</c>/<c>delayrate</c>) from
/// <see cref="PlayerEntity.EquipBonuses"/>. SC overlays (Suffragium / Memorize /
/// Slowcast / Paralysis / Izayoi / Bragi) live in <see cref="CastFixSc"/>. The
/// PER-SKILL cast tables (<c>sd-&gt;skillcastrate/skillvarcast/skillfixcast/
/// skilldelay</c>) need the bonus2 per-skill maps from COMBAT-22 → COMBAT-24;
/// SA_ABRACADABRA's abra_db is also COMBAT-24.
/// </summary>
public sealed class SkillCastTimingService : ISkillCastTimingService
{
    private readonly ISkillDb _db;
    private readonly IBattleConfigService _cfg;
    private readonly IStatusChangeService? _sc;

    public SkillCastTimingService(ISkillDb db, IBattleConfigService cfg, IStatusChangeService? sc = null)
    {
        _db = db;
        _cfg = cfg;
        _sc = sc;
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

        // Item / card bonuses are deferred per PARITY-REMAINING.md §P2.2 — PlayerEntity.
        // When the equip aggregator surfaces VariableCastrate / add_varcast
        // / skillcastrate they layer here.

        // Battle-config global rate.
        var rate = _cfg.GetValue("cast_rate");
        if (rate != 100) time = time * rate / 100;
        return Math.Max(0, (int)time);
    }

    // ----- skill_castfix_sc (pre-renewal SC overlay) -----------------
    //
    // rAthena reference: status.cpp:skill_castfix_sc + skill.cpp's
    // explicit SC reads. Order of operations (rAthena renewal):
    //   1) Slowcast / Paralysis push cast time *up*
    //   2) Suffragium / Memorize / Bragi / Izayoi push it *down*
    //   3) Suffragium + Memorize consume one charge per cast
    // The 'flag' param mirrors rAthena's hidden bypass bits — bit 0
    // skips Suffragium consumption (used by chained casts that share
    // one Suffragium); we keep the contract but currently honor only
    // bit 0 since no caller passes anything else.
    public int CastFixSc(Entity caster, int time, byte flag = 0)
    {
        if (time < 0) return 0;
        if (caster is MobEntity or NpcEntity) return time;
        if (_sc == null) return Math.Max(0, time);

        // Slowcast: time × (100 + 10 * val1) / 100. Stackable with
        // Paralysis since rAthena treats them as independent debuffs.
        var slow = _sc.Get(caster, StatusType.Slowcast);
        if (slow != null)
            time = time * (100 + 10 * slow.Val1) / 100;

        // Paralysis (Guillotine Cross). Val3 = extra cast %.
        var paralysis = _sc.Get(caster, StatusType.Paralysis);
        if (paralysis != null)
            time = time * (100 + paralysis.Val3) / 100;

        // Suffragium: time × (100 - 15 * val1) / 100. Auto-consumed
        // unless flag bit 0 says otherwise.
        var suffr = _sc.Get(caster, StatusType.Suffragium);
        if (suffr != null)
        {
            time = time * (100 - 15 * suffr.Val1) / 100;
            if ((flag & 1) == 0) _sc.End(caster, StatusType.Suffragium);
        }

        // Memorize: halves cast for the next val1 casts; decrement
        // and end at zero.
        var mem = _sc.Get(caster, StatusType.Memorize);
        if (mem != null)
        {
            time /= 2;
            if ((flag & 1) == 0)
            {
                mem.Val1--;
                if (mem.Val1 <= 0) _sc.End(caster, StatusType.Memorize);
            }
        }

        // Izayoi: halves variable cast (Kagerou / Oboro). Permanent
        // for the SC's duration — no per-cast consumption.
        if (_sc.Get(caster, StatusType.Izayoi) != null)
            time /= 2;

        // PoemBragi (Minstrel song). Val2 = combined rate
        // (6*lv + 3*caster_int), shaves cast and after-skill delay.
        var bragi = _sc.Get(caster, StatusType.Poembragi);
        if (bragi != null && bragi.Val2 > 0)
            time = time * Math.Max(0, 100 - bragi.Val2) / 100;

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

        // COMBAT-07: renewal DEX/INT variable-cast reduction + equip/card cast
        // bonuses (extracted to testable helpers below).
        var scale = _cfg.GetValue("vcast_stat_scale");
        var dexBypass = (_db.GetCastNoDex(skillId) & 1) != 0;
        var bundle = (caster as PlayerEntity)?.EquipBonuses;
        variableTime = ApplyVariableCast(variableTime, caster.Stats.Dex, caster.Stats.IntStat, scale, dexBypass, bundle);
        fixedTime = ApplyFixedCast(fixedTime, bundle);

        return Math.Max(0, variableTime + fixedTime);
    }

    /// <summary>
    /// COMBAT-07 — renewal variable-cast reduction. rAthena skill_vfcastfix
    /// (skill.cpp:20444): <c>time *= 1 - sqrt((dex*2+int)/vcast_stat_scale)</c>
    /// when DEX isn't bypassed, then the equip/card bonuses. Values are stored
    /// raw from the item script — a NEGATIVE bVariableCastrate is the common
    /// "faster cast" card, and bVariableCast (flat ms) is SUBTRACTED. Fixed
    /// cast is handled separately (it is NOT DEX/INT-reduced).
    /// </summary>
    internal static int ApplyVariableCast(int variableTime, int dex, int intStat, int vcastStatScale, bool dexBypass, EquipBonusBundle? b)
    {
        if (vcastStatScale > 0 && !dexBypass)
        {
            var reduce = Math.Sqrt((double)(dex * 2 + intStat) / vcastStatScale);
            if (reduce > 1) reduce = 1;
            variableTime = (int)(variableTime * (1 - reduce));
        }
        if (b != null)
        {
            variableTime -= b.AddVarCastMs;
            variableTime = variableTime * (100 + b.VarCastRate) / 100;
        }
        return variableTime < 0 ? 0 : variableTime;
    }

    /// <summary>COMBAT-07 — fixed-cast equip/card bonuses (no DEX/INT reduction).</summary>
    internal static int ApplyFixedCast(int fixedTime, EquipBonusBundle? b)
    {
        if (b != null)
        {
            fixedTime -= b.AddFixCastMs;
            fixedTime = fixedTime * (100 + b.FixCastRate) / 100;
        }
        return fixedTime < 0 ? 0 : fixedTime;
    }

    /// <summary>COMBAT-07 — after-cast delay equip/card reduction (bDelayrate, raw).</summary>
    internal static int ApplyDelayBonus(int time, EquipBonusBundle? b)
        => b == null ? time : Math.Max(0, time * (100 + b.DelayRate) / 100);

    // ----- skill_delayfix (after-cast delay) -------------------------
    public int DelayFix(Entity caster, ushort skillId, ushort skillLevel)
    {
        // SA_ABRACADABRA's 0-delay special case + the abra_db random-skill
        // table are out of scope here — tracked in COMBAT-24.

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

        // COMBAT-07: equip/card after-cast delay reduction (bDelayrate).
        return ApplyDelayBonus(Math.Max(0, (int)time), (caster as PlayerEntity)?.EquipBonuses);
    }
}
