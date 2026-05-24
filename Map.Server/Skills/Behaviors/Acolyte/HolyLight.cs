using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Skills.Behaviors.Mage;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AL_HOLYLIGHT — Acolyte Holy Light. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/holylight.cpp</c>.
///
/// <para>Single-target Holy magic. Ends SC_P_ALTER on the target
/// before the hit lands (rAthena clears Pneumatica Alter so the
/// strike isn't blocked). Routes through the standard magic-attack
/// pipeline.</para>
///
/// <para><see cref="CalculateSkillRatio"/> adds <c>+25 %</c> base;
/// when the caster has SC_SPIRIT with Val2 == SL_PRIEST (Soul Linker
/// Priest spirit), damage is multiplied by 5 (rAthena comment:
/// "Does 5x damage include bonuses from other skills?" — left as
/// faithful).</para>
/// </summary>
public sealed class HolyLight : SkillImpl
{
    private readonly Map.Server.Skills.ISkillAttackService? _skillAttack;

    public HolyLight() : base(SkillIds.AL_HOLYLIGHT) { }

    public HolyLight(Map.Server.Skills.ISkillAttackService? skillAttack = null) : base(SkillIds.AL_HOLYLIGHT)
    {
        _skillAttack = skillAttack;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: status_change_end(target, SC_P_ALTER);
        ctx.Sc?.End(target, StatusType.PAlter);

        // SL_PRIEST spirit multiplier (rAthena: if SC_SPIRIT.val2 == SL_PRIEST,
        // base_skillratio *= 5). Resolved here where ctx is available; the
        // CalculateSkillRatio path runs the +25 base bump.
        var spiritMul = 1;
        var spirit = ctx.Sc?.Get(src, StatusType.Spirit);
        if (spirit != null && spirit.Val2 == SkillIds.SL_PRIEST) spiritMul = 5;

        // rAthena: skill_attack(BF_MAGIC, src, src, target, getSkillId(), skill_lv, tick, flag);
        if (_skillAttack != null && spiritMul == 1)
        {
            _skillAttack.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
        }
        else
        {
            // Fallback path also handles the spirit-multiplier case so the 5x
            // bump lands consistently regardless of which damage pipeline
            // resolves the hit.
            var matk = MagicBoltHelper.PerHitDamage(src);
            var ratio = CalculateSkillRatio(100, src, target, skillLevel) * spiritMul;
            ctx.Damage.ApplyDamage(target, Math.Max(1, matk * ratio / 100), src);
        }
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: base_skillratio += 25. SL_PRIEST 5x multiplier is applied
        // in CastendDamageId (above) where the SC reader is available.
        return baseRatio + 25;
    }
}
