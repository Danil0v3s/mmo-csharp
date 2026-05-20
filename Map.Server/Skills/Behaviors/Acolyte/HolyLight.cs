using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AL_HOLYLIGHT — Acolyte Holy Light. Mirrors
/// <c>rathena-fork/src/map/skills/acolyte/holylight.cpp</c>.
///
/// Single-target Holy magic at 125 % MATK. The element fix vs the
/// target's defense element runs in the standard damage pipeline;
/// this plugin owns the cast claim + damage application.
/// </summary>
public sealed class HolyLight : SkillImpl
{
    public HolyLight() : base(SkillIds.AL_HOLYLIGHT) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var matk = (src.Stats.MatkMin + src.Stats.MatkMax) / 2;
        if (matk <= 0) matk = 1;
        var dmg = Math.Max(1, matk * 125 / 100);
        ctx.Damage.ApplyDamage(target, dmg, src);
    }
}
