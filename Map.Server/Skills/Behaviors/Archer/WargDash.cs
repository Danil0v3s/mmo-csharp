using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_WUGDASH — Ranger Warg Dash. Manual port of
/// <c>rathena-fork/src/map/skills/archer/wargdash.cpp</c>.
///
/// <para>Ratio: <c>+200</c>. Toggle skill: if SC_WUGDASH is already
/// active it ends; otherwise the SC starts (val2 = caster's direction).
/// Gated on the caster being warg-riding
/// (<see cref="PlayerOption.Wugrider"/>).</para>
/// </summary>
public sealed class WargDash : RecursiveDamageSplashSkillImpl
{
    public WargDash() : base(SkillIds.RA_WUGDASH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
        => baseRatio + 200;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc != null && ctx.Sc.Get(target, StatusType.Wugdash) != null)
        {
            ctx.Sc.End(target, StatusType.Wugdash);
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
            return;
        }
        if (src is PlayerEntity pc && (pc.Option & PlayerOption.Wugrider) != 0)
        {
            ctx.Sc?.Start(target, StatusType.Wugdash, val1: skillLevel, val2: 0, 0, 0, durationMs: 60_000, src);
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        }
    }
}
