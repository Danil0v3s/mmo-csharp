using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EM_EL_CRYSTAL_ARMOR — Elemental Crystal Armor. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/crystalarmor.cpp</c>.
/// Toggles SC_CRYSTAL_ARMOR on target + SC_CRYSTAL_ARMOR_OPTION on
/// the elemental.
/// </summary>
public sealed class CrystalArmor : SkillImpl
{
    public CrystalArmor() : base(SkillIds.EM_EL_CRYSTAL_ARMOR) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.CrystalArmor) != null
            || ctx.Sc?.Get(src, StatusType.CrystalArmorOption) != null)
        {
            ctx.Sc.End(target, StatusType.CrystalArmor);
            ctx.Sc.End(src, StatusType.CrystalArmorOption);
            return;
        }
        ctx.Sc?.Start(src, StatusType.CrystalArmorOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.CrystalArmor, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
