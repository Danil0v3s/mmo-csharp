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
///
/// <para>Party-iteration branch is TODO: requires a
/// <c>ForEachPartyMemberOnSameMap</c> helper that the map server
/// doesn't yet expose. Single-target / no-party path is faithful.</para>
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

        // TODO: party_foreachsamemap — when caster is in a party and
        // flag&1 is unset, rAthena iterates same-map party members and
        // recursively invokes self with flag|BCT_PARTY|1. Skipped
        // until IPartyMapService exposes the iterator.
    }
}
