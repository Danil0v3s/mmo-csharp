using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_INSTANTDEATH — Sage Instant Death (Suicide). Casts then kills the caster outright.
/// </summary>
public sealed class Suicide : SkillImpl
{
    public Suicide() : base(SkillIds.SA_INSTANTDEATH) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // rAthena: status_kill(src) — set HP to 0 + trigger death pipeline.
        if (src is PlayerEntity sd)
        {
            ctx.Damage?.ApplyDamage(sd, sd.Hp, sd);
        }
    }
}
