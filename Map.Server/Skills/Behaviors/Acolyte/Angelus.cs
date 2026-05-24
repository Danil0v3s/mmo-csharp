using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AL_ANGELUS — Acolyte Angelus. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/angelus.cpp</c>.
///
/// <para>Party-buff: applies <see cref="StatusType.Angelus"/> to
/// every party member in splash range when cast by a partied
/// player, or to the single target otherwise. rAthena uses the
/// <c>flag &amp; 1</c> bit to mark the inner per-member recursion
/// (so the same function ends up at the SC apply once the
/// iterator reaches each member).</para>
/// </summary>
public sealed class Angelus : SkillImpl
{
    public Angelus() : base(SkillIds.AL_ANGELUS) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena broadcasts only when target is in AOI of caster
        // (check_distance_bl(src, bl, AREA_SIZE)); our broadcaster
        // already scopes to AOI so the test is implicit.
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);

        // Duration ladder per AL_ANGELUS skill_db: 30 * (3 + lv) seconds.
        var duration = 30 * (3 + skillLevel) * 1000;
        ctx.Sc?.Start(target, StatusType.Angelus, val1: skillLevel, 0, 0, 0, duration, src);

        // rAthena party_foreachsamemap fan-out: when caster is in a party
        // (and we haven't already recursed via flag&1), apply the SC to
        // every same-map partymate. Routed through IPartyMapService;
        // includeSelf is false because the line above already covered
        // the explicit target.
        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc, m =>
            {
                if (m.Id.Value == target.Id.Value) return;
                ctx.Sc?.Start(m, StatusType.Angelus, val1: skillLevel, 0, 0, 0, duration, src);
            }, includeSelf: false);
        }
    }
}
