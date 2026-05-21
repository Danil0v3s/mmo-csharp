using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// RG_STRIPWEAPON — Divest Weapon. Manual port of
/// <c>rathena-fork/src/map/skills/thief/divestweapon.cpp</c>.
/// Strips the target's weapon via skill_strip_equip. Stripping
/// service is TODO — animation only.
/// </summary>
public sealed class DivestWeapon : SkillImpl
{
    public DivestWeapon() : base(SkillIds.RG_STRIPWEAPON) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
