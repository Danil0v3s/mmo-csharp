using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// AS_SONICBLOW — Sonic Blow. Manual port of
/// <c>rathena-fork/src/map/skills/thief/sonicblow.cpp</c>.
/// Renewal: <c>+100 + 100*lv</c> ratio, +50% if target HP &lt; 50%.
/// SL_ASSASIN linked stun bonus is TODO.
/// </summary>
public sealed class SonicBlow : WeaponSkillImpl
{
    public SonicBlow() : base(SkillIds.AS_SONICBLOW) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + 100 + 100 * skillLevel;
        if (target is PlayerEntity p && p.Hp < p.MaxHp / 2)
            ratio += ratio / 2;
        else if (target is MobEntity m && m.Hp < m.MaxHp / 2)
            ratio += ratio / 2;
        return ratio;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (System.Random.Shared.Next(100) < 4 * skillLevel + 20)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
    }
}
