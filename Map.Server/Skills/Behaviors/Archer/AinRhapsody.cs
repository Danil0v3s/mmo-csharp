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
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.AinRhapsody, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        // TODO: chorus-partner detection + party splash via skill_area_sub.
    }
}
