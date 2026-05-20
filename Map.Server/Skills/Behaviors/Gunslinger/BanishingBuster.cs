using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_BANISHING_BUSTER — auto-generated stub from
/// <c>src/map/skills/gunslinger/banishingbuster.hpp</c>.
///
/// <para>Inherits <see cref="WeaponSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class BanishingBuster : WeaponSkillImpl
{
    public BanishingBuster() : base(SkillIds.RL_BANISHING_BUSTER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // skillratio += -100 + 1000 + 200 * skill_lv;
    // 	RE_LVL_DMOD(100);
    return baseRatio;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // status_change* tsc = status_get_sc(target);
    // 	map_session_data* sd = BL_CAST(BL_PC, src);
    // 	map_session_data* dstsd = BL_CAST(BL_PC, target);
    // 
    // 	if (tsc == nullptr || tsc->empty()) {
    // 		return;
    // 	}
    // 
    // 	if (status_isimmune(target)) {
    // 		return;
    // 	}
    // 
    // 	if ((dstsd && (dstsd->class_ & MAPID_SECONDMASK) == MAPID_SOUL_LINKER) || rnd() % 100 >= 50 + 5 * skill_lv) {
    // 		if (sd) {
    // 			clif_skill_fail(*sd, getSkillId());
    // 		}
    // 		return;
    // 	}
    // 
    // 	uint16 n = skill_lv;
    // 
    // 	for (const auto& it : status_db) {
    // 		sc_type status = static_cast<sc_type>(it.first);
    // 		status_change_entry* sce = tsc->getSCE(status);
    // 
    // 		if (n <= 0) {
    // 			break;
    // 		}
    // 		if (sce == nullptr) {
    // 			continue;
    // 		}
    // 		if (it.second->flag[SCF_NOBANISHINGBUSTER]) {
    // 			continue;
    // 		}
    // 
    // 		switch (status) {
    // 			case SC_WHISTLE: case SC_ASSNCROS: case SC_POEMBRAGI:
    // 			case SC_APPLEIDUN: case SC_HUMMING: case SC_DONTFORGETME:
    // 			case SC_FORTUNE: case SC_SERVICE4U:
    // 				if (!battle_config.dispel_song || sce->val4 == 0) {
    // 					//If in song area don't end it, even if config enabled
    // 					continue;
    // 				}
    // 				break;
    // 			case SC_ASSUMPTIO:
    // 				if (target->type == BL_MOB) {
    // 					continue;
    // 				}
    // 				break;
    // 		}
    // 
    // 		if (status == SC_BERSERK || status == SC_SATURDAYNIGHTFEVER) {
    // 			sce->val2 = 0;
    // 		}
    // 		status_change_end(target, status);
    // 		n--;
    // 	}
    // 
    // 	if (dstsd) {
    // 		//Remove bonus_script by Banishing Buster
    // 		pc_bonus_script_clear(dstsd, BSF_REM_ON_BANISHING_BUSTER);
    // 	}
    }
}
