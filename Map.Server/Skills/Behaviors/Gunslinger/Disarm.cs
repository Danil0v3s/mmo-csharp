using Map.Server.Entities;
using Map.Server.Inventory;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_DISARM — Gunslinger Disarm. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/disarm.cpp</c>.
/// On hit, strips the target's weapon via skill_strip_equip.
/// </summary>
public sealed class Disarm : WeaponSkillImpl
{
    public Disarm() : base(SkillIds.GS_DISARM) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: skill_strip_equip(src, bl, GS_DISARM, skill_lv). Strip
        // duration scales with skill_lv via the SC table; pass a per-level
        // ms hint that the SC layer can override.
        ctx.SideEffect?.StripEquip(src, target, (int)EquipBits.HandR, durationMs: skillLevel * 5_000);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
