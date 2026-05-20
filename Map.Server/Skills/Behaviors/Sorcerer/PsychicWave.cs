using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Sorcerer;

/// <summary>
/// SO_PSYCHIC_WAVE — Sorcerer Psychic Wave. Mirrors
/// <c>rathena-fork/src/map/skills/sorcerer/psychicwave.cpp</c>.
///
/// Caster-centered Ghost magic AoE (radius 3). Per-victim
/// (700 + 800*lv)% MATK. Element-converted when caster has the
/// corresponding endow SC (Pyrotechnic / Aquaplay / etc.).
/// </summary>
public sealed class PsychicWave : RecursiveDamageSplashSkillImpl
{
    public PsychicWave() : base(SkillIds.SO_PSYCHIC_WAVE) { }

    public override short GetSplashSearchSize(Entity src, ushort skillLevel) => 3;

    public override long SplashDamage(Entity src, Entity victim, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var matk = (src.Stats.MatkMin + src.Stats.MatkMax) / 2;
        if (matk <= 0) matk = 1;
        var rate = 700 + 800 * skillLevel;
        return Math.Max(1, matk * rate / 100);
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => base.CastendPos2(src, src.X, src.Y, skillLevel, ctx); // caster-centered.
}
