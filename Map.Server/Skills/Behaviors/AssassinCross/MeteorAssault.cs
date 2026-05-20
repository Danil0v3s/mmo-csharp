using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.AssassinCross;

/// <summary>
/// ASC_METEORASSAULT — Assassin Cross Meteor Assault. Mirrors
/// <c>rathena-fork/src/map/skills/assassincross/meteorassault.cpp</c>.
///
/// Caster-centered splash (radius 2): per-victim (40 + 40*lv)% ATK.
/// Each victim rolls for stun / blind chance on hit.
/// </summary>
public sealed class MeteorAssault : RecursiveDamageSplashSkillImpl
{
    public MeteorAssault() : base(SkillIds.ASC_METEORASSAULT) { }

    public override short GetSplashSearchSize(Entity src, ushort skillLevel) => 2;

    public override long SplashDamage(Entity src, Entity victim, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var swing = ctx.Battle.CalcWeaponAttack(src, victim);
        var rate = 40 + 40 * skillLevel;
        return swing.Total * rate / 100;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => base.CastendPos2(src, src.X, src.Y, skillLevel, ctx); // caster-centered.
}
