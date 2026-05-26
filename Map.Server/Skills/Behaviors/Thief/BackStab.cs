using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Movement.UnitOps;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// RG_BACKSTAP — Back Stab. Manual port of
/// <c>rathena-fork/src/map/skills/thief/backstab.cpp</c>.
/// Renewal: 2 hits with dagger (div_ = 2). Ratio <c>+(200 + 40*lv)</c>,
/// halved when wielding a bow (battle_config.backstab_bow_penalty).
/// Before the swing the caster slips one cell behind the target via
/// <see cref="IUnitOpsService.CheckUnitMovePos"/>; the caster's
/// Hiding ends on cast. Renewal also rolls a 5 + 2*lv % stun on hit.
/// </summary>
public sealed class BackStab : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;
    private readonly IUnitOpsService? _unitOps;

    /// <summary>rAthena <c>W_DAGGER</c> — caster's WeaponType that turns
    /// Back Stab into a 2-hit attack on renewal.</summary>
    private const int W_DAGGER = 1;

    /// <summary>rAthena <c>W_BOW</c> — caster's WeaponType when wielding
    /// a bow; triggers the bow-penalty branch (ratio halved).</summary>
    private const int W_BOW = 9;

    public BackStab() : base(SkillIds.RG_BACKSTAP) { }

    public BackStab(ISkillAttackService? skillAttack = null, IUnitOpsService? unitOps = null)
        : base(SkillIds.RG_BACKSTAP)
    {
        _skillAttack = skillAttack;
        _unitOps = unitOps;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: bow wielders get (200 + 40*lv) / 2 when
        // backstab_bow_penalty is on (renewal default). Everyone else
        // gets the full bump.
        if (src is PlayerEntity pc && pc.WeaponType == W_BOW)
            return baseRatio + (200 + 40 * skillLevel) / 2;
        return baseRatio + 200 + 40 * skillLevel;
    }

    public override void ModifyDamageData(ref BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
        // Renewal: dagger casters land 2 hits.
        if (src is PlayerEntity pc && pc.WeaponType == W_DAGGER)
            dmg.Hits = 2;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena renewal: caster movepos to the cell behind the
        // target (computed from dir from src → target). On success the
        // caster's Hiding ends and the swing fires.
        _unitOps?.CheckUnitMovePos(src, target.X, target.Y, easy: 1);
        ctx.Sc?.End(src, StatusType.Hiding);
        _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);

        // applyAdditionalEffects: renewal stun roll.
        if (System.Random.Shared.Next(100) < 5 + 2 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 2_000, src);
    }
}
