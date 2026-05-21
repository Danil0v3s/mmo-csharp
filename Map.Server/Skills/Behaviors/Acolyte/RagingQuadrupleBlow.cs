using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// MO_CHAINCOMBO — Monk Raging Quadruple Blow. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/ragingquadrupleblow.cpp</c>.
///
/// <para>4-hit Knuckle combo. Renewal ratio: <c>+150 + 50*lv</c>;
/// doubled when caster wields a Knuckle. Ends caster's
/// <see cref="StatusType.Bladestop"/> on hit (Bladestop was the
/// combo precondition).</para>
/// </summary>
public sealed class RagingQuadrupleBlow : WeaponSkillImpl
{
    public RagingQuadrupleBlow() : base(SkillIds.MO_CHAINCOMBO) { }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
        // Knuckle weapon → multi-hit (4 hits). The "-6" in rAthena is
        // their sentinel for forced div; we just set Hits = 4.
        dmg.Hits = 4;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        base.CastendDamageId(src, target, skillLevel, ctx);
        ctx.Sc?.End(src, StatusType.Bladestop);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + 150 + 50 * skillLevel;
        // TODO: ×2 multiplier when caster weapon == W_KNUCKLE. Weapon-type
        // gate requires equip introspection on PlayerEntity.
        return ratio;
    }
}
