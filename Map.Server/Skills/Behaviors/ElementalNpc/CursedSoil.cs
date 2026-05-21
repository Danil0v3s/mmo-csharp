using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_CURSED_SOIL — Elemental Cursed Soil. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/cursedsoil.cpp</c>.
/// Toggles SC_CURSED_SOIL on target + SC_CURSED_SOIL_OPTION on the
/// elemental.
/// </summary>
public sealed class CursedSoil : SkillImpl
{
    public CursedSoil() : base(SkillIds.EL_CURSED_SOIL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.CursedSoil) != null
            || ctx.Sc?.Get(src, StatusType.CursedSoilOption) != null)
        {
            ctx.Sc.End(target, StatusType.CursedSoil);
            ctx.Sc.End(src, StatusType.CursedSoilOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.CursedSoilOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.CursedSoil, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
