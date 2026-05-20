using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BS_REPAIRWEAPON — auto-generated stub from
/// <c>src/map/skills/merchant/weaponrepair.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class WeaponRepair : SkillImpl
{
    public WeaponRepair() : base(SkillIds.BS_REPAIRWEAPON) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 	map_session_data* dstsd = BL_CAST(BL_PC, target);
    // 
    // 	if(sd && dstsd)
    // 		clif_item_repair_list( *sd, *dstsd, skill_lv );
    }
}
