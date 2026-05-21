using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// CG_SPECIALSINGER — Clown/Gypsy Special Singer. Manual port of
/// <c>rathena-fork/src/map/skills/archer/skilledspecialsinger.cpp</c>.
/// Cancels SC_ENSEMBLEFATIGUE on the target.
/// </summary>
public sealed class SkilledSpecialSinger : SkillImpl
{
    public SkilledSpecialSinger() : base(SkillIds.CG_SPECIALSINGER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.End(target, StatusType.Ensemblefatigue);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
