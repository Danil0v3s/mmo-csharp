using Map.Server.Elemental;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// EM_ELEMENTAL_BUSTER — Elemental Master Elemental Buster
/// (skill.cpp:EM_ELEMENTAL_BUSTER arm). Looks up the caster's bound
/// EM-tier elemental and dispatches to the matching sub-skill:
/// Ardor → Fire, Diluvio → Water, Procella → Wind, Terremotus → Ground,
/// Serpens → Poison. Refuses when no EM-tier is bound. Sub-skill
/// dispatch routes through <see cref="IUnitOpsService.SkillUseId"/> so
/// the standard damage pipeline runs.
/// </summary>
public sealed class ElementalBuster : SkillImpl
{
    public ElementalBuster() : base(SkillIds.EM_ELEMENTAL_BUSTER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        var subSkill = ResolveSubSkill(pc.ActiveElementalClassId);
        if (subSkill == 0)
        {
            ctx.Client?.BroadcastSkillFail(pc, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SummonNone);
            return;
        }
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.UnitOps?.SkillUseId(src, target.Id, subSkill, skillLevel);
    }

    private static ushort ResolveSubSkill(int classId) => classId switch
    {
        ElementalClassIds.Ardor      => SkillIds.EM_ELEMENTAL_BUSTER_FIRE,
        ElementalClassIds.Diluvio    => SkillIds.EM_ELEMENTAL_BUSTER_WATER,
        ElementalClassIds.Procella   => SkillIds.EM_ELEMENTAL_BUSTER_WIND,
        ElementalClassIds.Terremotus => SkillIds.EM_ELEMENTAL_BUSTER_GROUND,
        ElementalClassIds.Serpens    => SkillIds.EM_ELEMENTAL_BUSTER_POISON,
        _                            => (ushort)0,
    };
}
