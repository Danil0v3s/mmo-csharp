using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>
/// TK_MISSION — Mission. Manual port of
/// <c>rathena-fork/src/map/skills/taekwon/mission.cpp</c>.
/// Picks a random TaeKwon mission mob and stores it on the player.
/// Random-mob pick + script-variable persistence are TODO.
/// </summary>
public sealed class Mission : SkillImpl
{
    public Mission() : base(SkillIds.TK_MISSION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
