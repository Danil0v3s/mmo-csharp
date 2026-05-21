using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_DISARM — Gunslinger Disarm. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/disarm.cpp</c>.
/// On hit, strips the target's weapon. skill_strip_equip pipeline is TODO.
/// </summary>
public sealed class Disarm : WeaponSkillImpl
{
    public Disarm() : base(SkillIds.GS_DISARM) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // TODO: skill_strip_equip(src, target, GS_DISARM, skillLevel).
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
