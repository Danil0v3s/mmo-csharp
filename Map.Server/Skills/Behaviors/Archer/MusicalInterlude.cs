using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// TR_MUSICAL_INTERLUDE — Trouvere/Troubadour Musical Interlude.
/// Manual port of <c>rathena-fork/src/map/skills/archer/musicalinterlude.cpp</c>.
///
/// <para>Party-wide chorus buff. Target gets the SC, then every party
/// member on the same map gets it; chorus partner flips val3.</para>
/// </summary>
public sealed class MusicalInterlude : SkillImpl
{
    public MusicalInterlude() : base(SkillIds.TR_MUSICAL_INTERLUDE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var val3 = 0;
        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMapInRange(pcSrc, 14, m =>
            {
                if (m.Id.Value == pcSrc.Id.Value) return;
                val3 |= 2;
            }, includeSelf: false);
        }

        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.MusicalInterlude, val1: skillLevel, val2: 0, val3: val3, val4: 0, durationMs: 60_000, src);

        if (src is PlayerEntity pc2 && pc2.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pc2, m =>
            {
                if (m.Id.Value == target.Id.Value) return;
                ctx.Sc?.Start(m, StatusType.MusicalInterlude, val1: skillLevel, val2: 0, val3: val3, val4: 0, durationMs: 60_000, src);
            }, includeSelf: false);
        }
    }
}
