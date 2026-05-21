using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// DK_SERVANT_W_DEMOL — Dragon Knight Servant Weapon: Demolition.
/// Manual port of <c>rathena-fork/src/map/skills/swordman/servantweapondemolition.cpp</c>.
/// Ratio <c>+(-100 + 500*lv) + 5*POW</c>. Hit-count capped by the
/// caster's available servant balls — TODO.
/// </summary>
public sealed class ServantWeaponDemolition : RecursiveDamageSplashSkillImpl
{
    public ServantWeaponDemolition() : base(SkillIds.DK_SERVANT_W_DEMOL) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 500 * skillLevel) + 5 * src.Stats.Pow;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
