using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WH_HAWK_M — Wind Hawk Mastery. rAthena's hunter-falcon toggle
/// (<c>pc_setfalcon</c>) — flips <c>OPTION_FALCON</c> on / off so the
/// client renders the falcon sprite next to the caster. Cast on self,
/// no damage, no SC.
/// </summary>
public sealed class HawkMastery : SkillImpl
{
    public HawkMastery() : base(SkillIds.WH_HAWK_M) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        var hasFalcon = (pc.Option & PlayerOption.Falcon) != 0;
        ctx.Options?.SetFalcon(pc, !hasFalcon);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
