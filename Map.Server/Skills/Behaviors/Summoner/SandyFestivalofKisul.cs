using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SH_SANDY_FESTIVAL_OF_KI_SUL — Shaman Sandy Festival of Ki Sul.
/// Manual port of <c>rathena-fork/src/map/skills/summoner/sandyfestivalofkisul.cpp</c>.
/// Solo cast applies the festival buff SC (TODO — enum missing); party
/// splash is TODO. Communion doubles duration.
/// </summary>
public sealed class SandyFestivalofKisul : SkillImpl
{
    public SandyFestivalofKisul() : base(SkillIds.SH_SANDY_FESTIVAL_OF_KI_SUL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
