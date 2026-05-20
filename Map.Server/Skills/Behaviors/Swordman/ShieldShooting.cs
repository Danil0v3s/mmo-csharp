using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// IG_SHIELD_SHOOTING — auto-generated stub from
/// <c>src/map/skills/swordman/shieldshooting.hpp</c>.
///
/// <para>Inherits <see cref="RecursiveDamageSplashSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ShieldShooting : RecursiveDamageSplashSkillImpl
{
    public ShieldShooting() : base(SkillIds.IG_SHIELD_SHOOTING) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // const map_session_data* sd = BL_CAST(BL_PC, src);
    // 	const status_data* sstatus = status_get_status_data(*src);
    // 
    // 	skillratio += -100 + 1000 + 3500 * skill_lv;
    // 	skillratio += 10 * sstatus->pow;
    // 	skillratio += skill_lv * 150 * pc_checkskill(sd, IG_SHIELD_MASTERY);
    // 	if (sd) { // Damage affected by the shield's weight and refine. Need official formula. [Rytech]
    // 		int16 index = sd->equip_index[EQI_HAND_L];
    // 
    // 		if (index >= 0 && sd->inventory_data[index] && sd->inventory_data[index]->type == IT_ARMOR) {
    // 			skillratio += (sd->inventory_data[index]->weight * 7 / 6) / 10;
    // 			skillratio += sd->inventory.u.items_inventory[index].refine * 100;
    // 		}
    // 	}
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }
}
