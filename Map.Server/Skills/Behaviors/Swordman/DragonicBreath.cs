using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// DK_DRAGONIC_BREATH — Dragon Knight Dragonic Breath (skill.cpp:DK_DRAGONIC_BREATH).
/// Base ratio <c>baseRatio + (-100 + 50 + 350*lv) + 7*POW</c>. With
/// <c>SC_DRAGONIC_AURA</c> active: additional <c>+3*POW</c> and the
/// HP/SP block scales at 7 % per level; without: 5 % per level.
/// </summary>
public sealed class DragonicBreath : RecursiveDamageSplashSkillImpl
{
    public DragonicBreath() : base(SkillIds.DK_DRAGONIC_BREATH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + (-100 + 50 + 350 * skillLevel) + 7 * src.Stats.Pow;
        var auraActive = ctx.Sc?.Get(src, StatusType.DragonicAura) != null;
        // Per-level pct of (MaxHP/4 + MaxSP/2): 7 % with aura, 5 % without.
        var pct = auraActive ? 7 : 5;
        ratio += (skillLevel * (src.Stats.MaxHp * 25 / 100) * pct) / 100;
        ratio += (skillLevel * (src.Stats.MaxSp * 50 / 100) * pct) / 100;
        if (auraActive) ratio += 3 * src.Stats.Pow;
        return ratio;
    }
}
