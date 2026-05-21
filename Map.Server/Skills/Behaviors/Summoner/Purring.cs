using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_PURRING — Summoner Purring. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/purring.cpp</c>. SC enum +
/// party splash are TODO.
/// </summary>
public sealed class Purring : SkillImpl
{
    public Purring() : base(SkillIds.SU_PURRING) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
