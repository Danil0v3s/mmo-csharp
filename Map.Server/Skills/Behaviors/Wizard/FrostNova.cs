using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Wizard;

/// <summary>
/// WZ_FROSTNOVA — Wizard Frost Nova. Mirrors
/// <c>rathena-fork/src/map/skills/wizard/frostnova.cpp</c>.
///
/// Caster-centered Water magic AoE (radius 5). Per-victim damage
/// = (100 + 100*lv)% MATK + freeze chance (20 + 10*lv)%.
/// </summary>
public sealed class FrostNova : RecursiveDamageSplashSkillImpl
{
    private readonly Random _rng;

    public FrostNova(Random? rng = null) : base(SkillIds.WZ_FROSTNOVA)
    {
        _rng = rng ?? Random.Shared;
    }

    public override short GetSplashSearchSize(Entity src, ushort skillLevel) => 5;

    // Frost Nova is caster-centered, not target-centered.
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => base.CastendPos2(src, src.X, src.Y, skillLevel, ctx);

    public override long SplashDamage(Entity src, Entity victim, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var matk = (src.Stats.MatkMin + src.Stats.MatkMax) / 2;
        if (matk <= 0) matk = 1;
        var rate = 100 + 100 * skillLevel;
        return Math.Max(1, matk * rate / 100);
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return;
        var freezeChance = 20 + 10 * skillLevel;
        if (_rng.Next(100) < freezeChance)
        {
            ctx.Sc.Start(target, StatusType.Freeze, val1: 1, 0, 0, 0,
                durationMs: 3_000, src);
        }
    }
}
