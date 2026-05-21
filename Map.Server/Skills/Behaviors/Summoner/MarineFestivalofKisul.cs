using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SH_MARINE_FESTIVAL_OF_KI_SUL — Shaman Marine Festival of Ki Sul.
/// Manual port of <c>rathena-fork/src/map/skills/summoner/marinefestivalofkisul.cpp</c>.
/// Solo cast applies the festival buff SC (TODO — enum missing); party
/// splash is TODO. Communion doubles duration.
/// </summary>
public sealed class MarineFestivalofKisul : SkillImpl
{
    public MarineFestivalofKisul() : base(SkillIds.SH_MARINE_FESTIVAL_OF_KI_SUL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
