using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_CHILLY_AIR — Elemental Chilly Air. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/coolair.cpp</c>.
/// Toggles SC_CHILLY_AIR on target + SC_CHILLY_AIR_OPTION on the
/// elemental.
/// </summary>
public sealed class CoolAir : SkillImpl
{
    public CoolAir() : base(SkillIds.EL_CHILLY_AIR) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.ChillyAir) != null
            || ctx.Sc?.Get(src, StatusType.ChillyAirOption) != null)
        {
            ctx.Sc.End(target, StatusType.ChillyAir);
            ctx.Sc.End(src, StatusType.ChillyAirOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.ChillyAirOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.ChillyAir, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
