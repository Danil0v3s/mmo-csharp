using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_DARKCROW — Dark Claw / Dark Crow. Manual port of
/// <c>rathena-fork/src/map/skills/thief/darkclaw.cpp</c>.
/// Ratio <c>+100*(lv-1)</c>. SC_DARKCROW lands on the target after
/// the swing regardless of hit / miss — rAthena's comment notes the
/// SC is applied even on miss, so we route it through CastendDamageId
/// instead of ApplyAdditionalEffects (which only runs on hit).
/// </summary>
public sealed class DarkClaw : WeaponSkillImpl
{
    public DarkClaw() : base(SkillIds.GC_DARKCROW) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 * (skillLevel - 1);

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        base.CastendDamageId(src, target, skillLevel, ctx);
        // sc_start(src, target, SC_DARKCROW, 100, skill_lv, skill_get_time(...)).
        // rAthena duration table: 10s @ lv 1, +5s per lv.
        ctx.Sc?.Start(target, StatusType.Darkcrow, val1: skillLevel, 0, 0, 0,
            durationMs: 5_000 + 5_000 * skillLevel, src);
    }
}
