using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_POISONINGWEAPON — Poisoning Weapon. Manual port of
/// <c>rathena-fork/src/map/skills/thief/poisoningweapon.cpp</c>.
/// Opens the poison-list selection dialog. Dialog wiring is TODO.
/// </summary>
public sealed class PoisoningWeapon : SkillImpl
{
    public PoisoningWeapon() : base(SkillIds.GC_POISONINGWEAPON) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
