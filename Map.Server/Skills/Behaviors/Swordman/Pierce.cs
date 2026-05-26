using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// KN_PIERCE — Knight Pierce. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/pierce.cpp</c>.
///
/// <para>Ratio: <c>+10*lv</c>; doubles when the caster carries
/// <see cref="StatusType.ChargingpierceCount"/> with <c>val1 ≥ 10</c>.
/// Hit chance bonus <c>+5*lv %</c>. Multi-hit divisor scales with
/// target size+1 (rAthena <c>modifyDamageData</c>).</para>
/// </summary>
public sealed class Pierce : WeaponSkillImpl
{
    public Pierce() : base(SkillIds.KN_PIERCE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var ratio = baseRatio + 10 * skillLevel;
        var charging = ctx.Sc?.Get(src, StatusType.ChargingpierceCount);
        if (charging != null && charging.Val1 >= 10) ratio *= 2;
        return ratio;
    }

    public override short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
        => (short)(hitRate + hitRate * 5 * skillLevel / 100);

    public override void ModifyDamageData(ref BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: div_ = size+1 (Small=0 → 1 hit, Medium=1 → 2, Large=2 → 3).
        var size = (int)target.Stats.Size + 1;
        dmg.Hits = dmg.Hits > 0 ? size : -size;
    }
}
