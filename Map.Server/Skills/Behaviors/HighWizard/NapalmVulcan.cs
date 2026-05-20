using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.HighWizard;

/// <summary>
/// HW_NAPALMVULCAN — High Wizard Napalm Vulcan. Mirrors
/// <c>rathena-fork/src/map/skills/highwizard/napalmvulcan.cpp</c>.
///
/// Ghost-element AoE (3×3) — like Napalm Beat but each victim takes
/// full damage instead of split. Per-victim = (70 + 70*lv)% MATK.
/// </summary>
public sealed class NapalmVulcan : RecursiveDamageSplashSkillImpl
{
    public NapalmVulcan() : base(SkillIds.HW_NAPALMVULCAN) { }

    public override short GetSplashSearchSize(Entity src, ushort skillLevel) => 1;

    public override long SplashDamage(Entity src, Entity victim, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var matk = (src.Stats.MatkMin + src.Stats.MatkMax) / 2;
        if (matk <= 0) matk = 1;
        var rate = 70 + 70 * skillLevel;
        return Math.Max(1, matk * rate / 100);
    }
}
