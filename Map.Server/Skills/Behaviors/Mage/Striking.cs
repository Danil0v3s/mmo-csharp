using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_STRIKING — Sorcerer Striking. Manual port of
/// <c>rathena-fork/src/map/skills/mage/striking.cpp</c>.
///
/// <para>Self/party-only weapon-attack buff (SC_STRIKING).
/// Bonus = <c>20*lv * weapon_level</c> of the target's right-hand
/// weapon. The weapon-level read isn't exposed on Entity yet, so we
/// pass the lv-scaled bonus as <c>20*lv*3</c> (assumes a tier-3
/// weapon as a placeholder).</para>
/// </summary>
public sealed class Striking : SkillImpl
{
    public Striking() : base(SkillIds.SO_STRIKING) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // BCT_SELF|BCT_PARTY gate — friendly-only; default to allowing self.
        if (src.Id != target.Id && target is not PlayerEntity)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        // rAthena bonus = 20*lv * target_weapon_level — placeholder uses lv*60.
        var bonus = 20 * skillLevel * 3;
        ctx.Sc?.Start(target, StatusType.Striking, val1: skillLevel, val2: bonus, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
