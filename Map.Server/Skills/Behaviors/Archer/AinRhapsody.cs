using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// TR_AIN_RHAPSODY — Troubadour/Trouvere Ain Rhapsody. Manual port of
/// <c>rathena-fork/src/map/skills/archer/ainrhapsody.cpp</c>.
///
/// <para>Chorus debuff. The named target gets SC_AIN_RHAPSODY, then a
/// splash via <see cref="IEntityRegistry.ForEachInRange"/> on the
/// caster's coords applies the SC to every nearby enemy (BL_CHAR).
/// A chorus-partner within AREA_SIZE doubles the val3 magnitude
/// (rAthena <c>skill_check_pc_partner</c>) — partner detection rides
/// on <see cref="Party.IPartyMapService.ForEachOnSameMapInRange"/>.</para>
/// </summary>
public sealed class AinRhapsody : SkillImpl
{
    public AinRhapsody() : base(SkillIds.TR_AIN_RHAPSODY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        const short splashRange = 7; // skill_db splash for TR_AIN_RHAPSODY

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
        ctx.Sc?.Start(target, StatusType.AinRhapsody, val1: skillLevel, val2: 0, val3: val3, val4: 0, durationMs: 30_000, src);

        var nearby = ctx.Entities.ForEachInRange(src.MapId, src.X, src.Y, splashRange, EntityType.Mob | EntityType.Pc);
        foreach (var bl in nearby)
        {
            if (bl.Id.Value == src.Id.Value) continue;
            if (bl.Id.Value == target.Id.Value) continue;
            ctx.Sc?.Start(bl, StatusType.AinRhapsody, val1: skillLevel, val2: 0, val3: val3, val4: 0, durationMs: 30_000, src);
        }
    }
}
