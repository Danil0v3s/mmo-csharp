using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// SM_BASH — Swordsman Bash. Mirrors
/// <c>rathena-fork/src/map/skills/swordman/bash.cpp</c>.
///
/// <list type="bullet">
///   <item>Damage ratio: +30 % per skill level.</item>
///   <item>Hit rate: +5 % per skill level.</item>
///   <item>Fatal Blow stun proc at lv 6+ (rAthena gates on
///         <c>pc_checkskill(SM_FATALBLOW)</c>; the C# port doesn't
///         track Fatal Blow learning yet, so we keep the lv ≥ 6
///         simplification).</item>
/// </list>
/// </summary>
public sealed class Bash : WeaponSkillImpl
{
    private const int StunDurationMs = 5_000;

    public Bash() : base(SkillIds.SM_BASH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 30 * skillLevel;

    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
        => (short)(hitRate + hitRate * 5 * skillLevel / 100);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (skillLevel < 6 || ctx.Sc == null) return;
        var stunChance = 5 + 5 * (skillLevel - 5); // lv6=10% .. lv10=30%
        // SKILL-01: raw rate (stunChance% → *100); the SC engine runs
        // status_get_sc_def (VIT resist + level-diff + boss immunity) + rolls.
        ctx.Sc.Start(target, StatusType.Stun, rate: stunChance * 100,
            val1: 1, 0, 0, 0, StunDurationMs, source: src);
    }
}
