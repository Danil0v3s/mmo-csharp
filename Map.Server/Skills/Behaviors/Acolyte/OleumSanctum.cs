using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// IQ_OLEUM_SANCTUM — Inquisitor Oleum Sanctum. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/oleumsanctum.cpp</c>.
///
/// <para>Single-cast Holy splash that broadcasts the cast frame
/// then runs the damage pipeline + applies <see cref="StatusType.HolyOil"/>
/// to victims on hit (skill_db Duration1).</para>
///
/// <para>Ratio: <c>-100 + 500 + 2000*lv + 5*POW</c>.</para>
/// </summary>
public sealed class OleumSanctum : RecursiveDamageSplashSkillImpl
{
    public OleumSanctum() : base(SkillIds.IQ_OLEUM_SANCTUM) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        CastendDamageId(src, target, skillLevel, ctx);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 500 + 2000 * skillLevel) + 5 * src.Stats.Pow;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: sc_start(src, target, SC_HOLY_OIL, 100, skill_lv, skill_get_time(...));
        // skill_db Duration1 — 6 s baseline at lv 1.
        ctx.Sc?.Start(target, StatusType.HolyOil, val1: skillLevel,
            0, 0, 0, durationMs: 6000, src);
    }
}
