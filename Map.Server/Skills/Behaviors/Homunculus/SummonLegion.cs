using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_SUMMON_LEGION — Homunculus Summon Legion. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_summonlegion.cpp</c>.
/// Spawns 3-5 hornet / luciola vespa slaves keyed to skill level.
/// Slave spawn + count cap are TODO.
/// </summary>
public sealed class SummonLegion : SkillImpl
{
    public SummonLegion() : base(SkillIds.MH_SUMMON_LEGION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
}
