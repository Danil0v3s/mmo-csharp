using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_MEOWMEOW — Summoner Meow Meow. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/meowmeow.cpp</c>. Buff +
/// party splash; SC enum is missing — TODO.
/// </summary>
public sealed class MeowMeow : SkillImpl
{
    public MeowMeow() : base(SkillIds.SU_MEOWMEOW) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
