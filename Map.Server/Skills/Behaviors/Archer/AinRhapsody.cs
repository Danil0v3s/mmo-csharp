using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// TR_AIN_RHAPSODY — Troubadour/Trouvere Ain Rhapsody. Manual port of
/// <c>rathena-fork/src/map/skills/archer/ainrhapsody.cpp</c>.
///
/// <para>Performer chorus debuff. Applies SC_AIN_RHAPSODY at 100 %
/// across the splash. Pair-doubled (BCT_PARTY chorus partner) boosts
/// the val2 flag — partner search isn't wired here. Splash via
/// map_foreachinallrange is TODO; the named target gets the SC.</para>
/// </summary>
public sealed class AinRhapsody : SkillImpl
{
    public AinRhapsody() : base(SkillIds.TR_AIN_RHAPSODY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Chorus-partner detection: if any same-map party member is in
        // splash range, val3 bit 1 doubles the SC magnitude (rAthena
        // sc->val3 & 2 read in status.cpp:12529-12530).
        var val3 = 0;
        const short splashRange = 7; // skill_db splash for TR_AIN_RHAPSODY
        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMapInRange(pcSrc, splashRange, m =>
            {
                if (m.Id.Value == pcSrc.Id.Value) return;
                val3 |= 2;
            }, includeSelf: false);
        }

        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.AinRhapsody, val1: skillLevel, val2: 0, val3: val3, val4: 0, durationMs: 30_000, src);

        // rAthena map_foreachinallrange splash: every enemy in splashRange
        // gets the debuff. We approximate with the entity registry +
        // BCT_ENEMY filter (mobs only for now).
        var nearby = ctx.Entities.ForEachInRange(src.MapId, target.X, target.Y, splashRange, Map.Server.Entities.EntityType.Mob);
        foreach (var bl in nearby)
        {
            if (bl.Id.Value == target.Id.Value) continue;
            ctx.Sc?.Start(bl, StatusType.AinRhapsody, val1: skillLevel, val2: 0, val3: val3, val4: 0, durationMs: 30_000, src);
        }
    }
}
