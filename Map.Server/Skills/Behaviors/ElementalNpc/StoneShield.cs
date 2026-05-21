using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_STONE_SHIELD — Elemental Stone Shield. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/stoneshield.cpp</c>.
/// Toggles SC_STONE_SHIELD on target + SC_STONE_SHIELD_OPTION on the
/// elemental.
/// </summary>
public sealed class StoneShield : SkillImpl
{
    public StoneShield() : base(SkillIds.EL_STONE_SHIELD) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.StoneShield) != null
            || ctx.Sc?.Get(src, StatusType.StoneShieldOption) != null)
        {
            ctx.Sc.End(target, StatusType.StoneShield);
            ctx.Sc.End(src, StatusType.StoneShieldOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.StoneShieldOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.StoneShield, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
