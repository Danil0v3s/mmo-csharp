using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Monk;

/// <summary>
/// MO_EXPLOSIONSPIRITS — Monk Explosion Spirits. Mirrors
/// <c>rathena-fork/src/map/skills/monk/explosionspirits.cpp</c>.
///
/// Apply <see cref="StatusType.Explosionspirits"/> on caster.
/// Val1 = lv*20 crit boost, Val2 = lv*50 ATK boost. Duration 180 s.
/// Required prereq for Asura Strike.
/// </summary>
public sealed class ExplosionSpirits : SkillImpl
{
    public ExplosionSpirits() : base(SkillIds.MO_EXPLOSIONSPIRITS) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(src, StatusType.Explosionspirits,
            val1: skillLevel * 20, val2: skillLevel * 50, val3: 0, val4: 0,
            durationMs: 180_000, src);
    }
}
