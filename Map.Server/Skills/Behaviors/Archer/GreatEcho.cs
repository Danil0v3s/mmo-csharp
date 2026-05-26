using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_GREAT_ECHO — Minstrel/Wanderer Great Echo. Manual port of
/// <c>rathena-fork/src/map/skills/archer/greatecho.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 250 + 500*lv) + 50*WM_LESSON</c>; if a
/// chorus partner is within AREA_SIZE (14), the running ratio is
/// doubled. Partner check uses
/// <see cref="Party.IPartyMapService.ForEachOnSameMapInRange"/> as a
/// stand-in for rAthena <c>skill_check_pc_partner</c>.</para>
/// </summary>
public sealed class GreatEcho : WeaponSkillImpl
{
    public GreatEcho() : base(SkillIds.WM_GREAT_ECHO) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var ratio = baseRatio + (-100 + 250 + 500 * skillLevel);
        if (src is PlayerEntity pc)
        {
            var lesson = ctx.PlayerSkill?.CheckSkill(pc, SkillIds.WM_LESSON) ?? 0;
            ratio += lesson * 50;
            if (pc.PartyId > 0 && ctx.PartyMap != null)
            {
                var partners = 0;
                ctx.PartyMap.ForEachOnSameMapInRange(pc, 14, m =>
                {
                    if (m.Id.Value != pc.Id.Value) partners++;
                }, includeSelf: false);
                if (partners > 0) ratio *= 2;
            }
        }
        return ratio;
    }
}
