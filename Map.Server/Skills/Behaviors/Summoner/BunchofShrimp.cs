using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_BUNCHOFSHRIMP — Summoner Bunch of Shrimp. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/bunchofshrimp.cpp</c>.
/// Buffs target with the bunch-of-shrimp SC. SU_SPIRITOFSEA duration
/// extension + party splash are TODO.
/// </summary>
public sealed class BunchofShrimp : SkillImpl
{
    public BunchofShrimp() : base(SkillIds.SU_BUNCHOFSHRIMP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // TODO: SC enum (Shrimpblessing exists at line 685 — verify) and party splash.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
