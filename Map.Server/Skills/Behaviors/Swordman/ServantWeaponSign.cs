using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// DK_SERVANT_W_SIGN — Dragon Knight Servant Weapon: Sign. Manual port
/// of <c>rathena-fork/src/map/skills/swordman/servantweaponsign.cpp</c>.
/// Marks the target with SC_SERVANT_SIGN keyed to the caster.
/// Multi-target slot management (MAX_SERVANT_SIGN) is TODO.
/// </summary>
public sealed class ServantWeaponSign : SkillImpl
{
    public ServantWeaponSign() : base(SkillIds.DK_SERVANT_W_SIGN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Reject if target is already marked by someone else.
        var existing = ctx.Sc?.Get(target, StatusType.ServantSign);
        if (existing != null && existing.Val1 != (int)src.Id) return;
        ctx.Sc?.Start(target, StatusType.ServantSign, val1: (int)src.Id, val2: 0, val3: skillLevel, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
