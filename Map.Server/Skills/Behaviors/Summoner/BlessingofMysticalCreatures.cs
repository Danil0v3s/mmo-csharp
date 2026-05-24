using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SH_BLESSING_OF_MYSTICAL_CREATURES — Shaman Blessing of Mystical
/// Creatures. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/blessingofmysticalcreatures.cpp</c>.
/// Heals AP up to 200 and applies SC_BLESSING_OF_M_CREATURES.
/// </summary>
public sealed class BlessingofMysticalCreatures : SkillImpl
{
    public BlessingofMysticalCreatures() : base(SkillIds.SH_BLESSING_OF_MYSTICAL_CREATURES) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // AP heal up to 200 — `Ap` is the 4th-class active-power pool
        // on PlayerEntity (added in AT-C wave).
        if (target is PlayerEntity tpc)
        {
            tpc.Ap = Math.Min(tpc.MaxAp, tpc.Ap + 200);
        }
        // SC apply — enum name is BlessingOfMCreatures (status.yml).
        ctx.Sc?.Start(target, StatusType.BlessingOfMCreatures, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
