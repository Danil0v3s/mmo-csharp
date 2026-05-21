using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_ANTIDOTE — Antidote. Manual port of
/// <c>rathena-fork/src/map/skills/thief/antidote.cpp</c>.
/// Removes Paralysis, Pyrexia, DeathHurt, LeechEsd, VenomBleed,
/// MagicMushroom, Toxin, OblivionCurse from the target.
/// </summary>
public sealed class Antidote : SkillImpl
{
    public Antidote() : base(SkillIds.GC_ANTIDOTE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.End(target, StatusType.Paralyse);
        ctx.Sc?.End(target, StatusType.Pyrexia);
        ctx.Sc?.End(target, StatusType.Deathhurt);
        ctx.Sc?.End(target, StatusType.Leechesend);
        ctx.Sc?.End(target, StatusType.Venombleed);
        ctx.Sc?.End(target, StatusType.Magicmushroom);
        ctx.Sc?.End(target, StatusType.Toxin);
        ctx.Sc?.End(target, StatusType.Oblivioncurse);
    }
}
