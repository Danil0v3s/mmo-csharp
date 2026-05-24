using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// WE_BABY — Baby cry-for-help. Manual port of
/// <c>rathena-fork/src/map/skills/other/baby.cpp</c>.
/// Stuns the caster (baby) and applies the adoption SC to father /
/// mother. Adoption family lookup is TODO; we land the animation.
/// </summary>
public sealed class Baby : SkillImpl
{
    public Baby() : base(SkillIds.WE_BABY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity baby) return;

        // Always stun the baby.
        ctx.Sc?.Start(src, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);

        // rAthena pc_get_father / pc_get_mother: char-server holds the
        // adoption table; cross-server lookup not surfaced on PlayerEntity
        // yet. As a pragmatic fallback, apply SC_BABY to every same-map
        // partymate (the parents are normally in the same party with the
        // baby). When the adoption IPC ports, swap this for the exact
        // father/mother CharId lookup.
        if (baby.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(baby, m =>
            {
                if (m.Id.Value == baby.Id.Value) return;
                ctx.Sc?.Start(m, StatusType.Wedding, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
            }, includeSelf: false);
        }
    }
}
