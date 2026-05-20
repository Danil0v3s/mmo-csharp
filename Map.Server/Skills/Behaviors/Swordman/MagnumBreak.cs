using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// SM_MAGNUM — Swordsman Magnum Break. Mirrors
/// <c>rathena-fork/src/map/skills/swordman/magnumbreak.cpp</c>.
///
/// 360° splash (radius 2) around the caster — every enemy takes a
/// (120 + 20*lv)% physical hit. After the splash, applies
/// <see cref="StatusType.Fireweapon"/> on the caster for 10 s,
/// endowing auto-attacks with Fire.
/// </summary>
public sealed class MagnumBreak : RecursiveDamageSplashSkillImpl
{
    private const int FireWeaponDurationMs = 10_000;

    public MagnumBreak() : base(SkillIds.SM_MAGNUM) { }

    public override short GetSplashSearchSize(Entity src, ushort skillLevel) => 2;

    public override long SplashDamage(Entity src, Entity victim, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var rate = 120 + 20 * skillLevel;
        var swing = ctx.Battle.CalcWeaponAttack(src, victim);
        return swing.Total * rate / 100;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Magnum splashes around the CASTER, not the target.
        base.CastendDamageId(src, src, skillLevel, ctx);
        ApplyFireWeapon(src, skillLevel, ctx);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        base.CastendPos2(src, x, y, skillLevel, ctx);
        ApplyFireWeapon(src, skillLevel, ctx);
    }

    private static void ApplyFireWeapon(Entity src, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(src, StatusType.Fireweapon, val1: skillLevel, 0, 0, 0,
            FireWeaponDurationMs);
    }
}
