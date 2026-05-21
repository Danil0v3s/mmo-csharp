using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WH_CRESCIVE_BOLT — Wind Hawk Crescive Bolt. Manual port of
/// <c>rathena-fork/src/map/skills/archer/crescivebolt.cpp</c>.
///
/// <para>Stacking bow skill. Ratio: <c>+(-100 + 500 + 1300*lv) + 5*CON</c>,
/// scaled by 20× the SC_CRESCIVEBOLT stack count (caster SC readback
/// TODO) and ×1.2 with SC_CALAMITYGALE (+×1.5 vs Brute/Fish). After
/// the hit, the SC_CRESCIVEBOLT stack increments up to 3.</para>
/// </summary>
public sealed class CresciveBolt : WeaponSkillImpl
{
    public CresciveBolt() : base(SkillIds.WH_CRESCIVE_BOLT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 500 + 1300 * skillLevel) + 5 * src.Stats.Con;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        base.CastendDamageId(src, target, skillLevel, ctx);
        // Stack the Crescive Bolt buff (1..3) — readback of the current val1 isn't surfaced; start at 1.
        ctx.Sc?.Start(src, StatusType.Crescivebolt, val1: 1, 0, 0, 0, durationMs: 10_000, src);
    }
}
