using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_LEVELUP — Sage Leveling (Hocus Pocus). Grants the caster
/// 10 % of next-base-exp toward level up.
/// </summary>
public sealed class Leveling : SkillImpl
{
    private readonly IExpService? _exp;
    public Leveling() : base(SkillIds.SA_LEVELUP) { }
    public Leveling(IExpService? exp = null) : base(SkillIds.SA_LEVELUP) => _exp = exp;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // rAthena: pc_gainexp(sd, null, nextbaseexp * 10 / 100, 0, 0).
        // We don't have per-class next-exp lookup surfaced here — grant
        // a flat 10*level exp as a baseline placeholder.
        if (src is PlayerEntity sd)
        {
            _exp?.GainExp(sd, baseExp: sd.Level * 10, jobExp: 0);
        }
    }
}
