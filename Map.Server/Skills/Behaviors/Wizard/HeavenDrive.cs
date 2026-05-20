using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Wizard;

/// <summary>
/// WZ_HEAVENDRIVE — Wizard Heaven's Drive. Mirrors
/// <c>rathena-fork/src/map/skills/wizard/heavensdrive.cpp</c>.
///
/// 5×5 Earth magic AoE. Per-victim damage = (125 + 25*lv)% MATK.
/// Reveals hidden / cloaked targets in the splash.
/// </summary>
public sealed class HeavenDrive : RecursiveDamageSplashSkillImpl
{
    public HeavenDrive() : base(SkillIds.WZ_HEAVENDRIVE) { }

    public override short GetSplashSearchSize(Entity src, ushort skillLevel) => 2;

    public override long SplashDamage(Entity src, Entity victim, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var matk = (src.Stats.MatkMin + src.Stats.MatkMax) / 2;
        if (matk <= 0) matk = 1;
        var rate = 125 + 25 * skillLevel;
        return Math.Max(1, matk * rate / 100);
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Reveal hidden / cloaked victims (end SC before next tick).
        ctx.Sc?.End(target, StatusType.Hiding);
        ctx.Sc?.End(target, StatusType.Cloaking);
    }
}
