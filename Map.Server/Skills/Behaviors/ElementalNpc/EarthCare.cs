using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EM_EL_EARTH_CARE — Elemental Earth Care. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/earthcare.cpp</c>.
/// Toggles SC_EARTH_CARE on target + SC_EARTH_CARE_OPTION on the
/// elemental.
/// </summary>
public sealed class EarthCare : SkillImpl
{
    public EarthCare() : base(SkillIds.EM_EL_EARTH_CARE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.EarthCare) != null
            || ctx.Sc?.Get(src, StatusType.EarthCareOption) != null)
        {
            ctx.Sc.End(target, StatusType.EarthCare);
            ctx.Sc.End(src, StatusType.EarthCareOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.EarthCareOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.EarthCare, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
