using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_RECUPERATE — Mercenary Recuperate. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_recuperate.cpp</c>.
/// Cleanses SC_POISON, SC_DPOISON, SC_SILENCE.
/// </summary>
public sealed class MercenaryRecuperate : SkillImpl
{
    public MercenaryRecuperate() : base(SkillIds.MER_RECUPERATE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.End(target, StatusType.Poison);
        ctx.Sc?.End(target, StatusType.DeadlyPoison);
        ctx.Sc?.End(target, StatusType.Silence);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
