using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// DK_SERVANTWEAPON — Dragon Knight Servant Weapon. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/servantweapon.cpp</c>.
/// Starts SC_SERVANTWEAPON with val2 = caster id.
/// </summary>
public sealed class ServantWeapon : SkillImpl
{
    public ServantWeapon() : base(SkillIds.DK_SERVANTWEAPON) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Servantweapon, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
