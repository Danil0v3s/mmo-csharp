using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_SCAPEGOAT — Mercenary Scapegoat (skill.cpp:MER_SCAPEGOAT arm).
/// Sacrifices the mercenary by transferring its current HP to the
/// master (heal), then dealing the mercenary's MaxHp as damage to
/// itself (kills it). Master lookup goes through
/// <see cref="Entity.MasterId"/>.
/// </summary>
public sealed class MercenaryScapegoat : SkillImpl
{
    public MercenaryScapegoat() : base(SkillIds.MER_SCAPEGOAT) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (src.MasterId is not { } masterId) return;
        var master = ctx.Entities.Get(masterId);
        if (master is not PlayerEntity masterPc) return;
        var transferredHp = src.Stats.Hp;
        ctx.StatusOps?.Heal(masterPc, transferredHp, 0, 2);
        // Kill the mercenary by applying its full MaxHp as damage.
        ctx.Damage?.ApplyDamage(src, src.Stats.MaxHp, src);
    }
}
