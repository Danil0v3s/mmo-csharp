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
        // rAthena SM_MAGNUM (battle.cpp:4644): splash is centred on the
        // caster; the inner 3×3 (Chebyshev distance ≤ 1, rAthena miscflag==1)
        // takes 100 + 20*lv, the outer 5×5 ring takes 100 + 10*lv. The old
        // flat 120 + 20*lv used the wrong base and ignored the inner/outer split.
        var dist = Math.Max(Math.Abs(src.X - victim.X), Math.Abs(src.Y - victim.Y));
        var rate = dist <= 1 ? 100 + 20 * skillLevel : 100 + 10 * skillLevel;
        // COMBAT-96 — skill-aware swing + the ÷200 skill crit_atk_rate bump (battle.cpp:7787).
        var swing = ctx.Battle.CalcWeaponAttack(src, victim, SkillId);
        return ApplySkillCritAtkRate((long)swing.Total * rate / 100, src, swing);
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
