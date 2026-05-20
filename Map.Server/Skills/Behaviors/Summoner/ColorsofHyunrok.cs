using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SH_COLORS_OF_HYUN_ROK — auto-generated stub from
/// <c>src/map/skills/summoner/colorsofhyunrok.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class ColorsofHyunrok : SkillImpl
{
    public ColorsofHyunrok() : base(SkillIds.SH_COLORS_OF_HYUN_ROK) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (skill_lv == 7) {
    // 		status_change_end(src, SC_COLORS_OF_HYUN_ROK_1);
    // 		status_change_end(src, SC_COLORS_OF_HYUN_ROK_2);
    // 		status_change_end(src, SC_COLORS_OF_HYUN_ROK_3);
    // 		status_change_end(src, SC_COLORS_OF_HYUN_ROK_4);
    // 		status_change_end(src, SC_COLORS_OF_HYUN_ROK_5);
    // 		status_change_end(src, SC_COLORS_OF_HYUN_ROK_6);
    // 		// The skill also ends the buff that increases Catnip Meteor damage
    // 		status_change_end(src, SC_COLORS_OF_HYUN_ROK_BUFF);
    // 
    // 		clif_skill_nodamage(src, *src, getSkillId(), skill_lv);
    // 	}
    // 	else {
    // 		map_session_data* sd = BL_CAST(BL_PC, src);
    // 		status_change *sc = status_get_sc(src);
    // 		sc_type type = skill_get_sc(getSkillId());
    // 
    // 		// Buff to increase Catnip Meteor damage
    // 		if( pc_checkskill( sd, SH_COMMUNE_WITH_HYUN_ROK ) > 0 || ( sc != nullptr && sc->getSCE( SC_TEMPORARY_COMMUNION ) != nullptr ) )
    // 			sc_start(src, target, SC_COLORS_OF_HYUN_ROK_BUFF, 100, 1, skill_get_time(getSkillId(), skill_lv));
    // 
    // 		// Endows elemental property to Catnip Meteor, Hyunrok Breeze and Hyunrok Cannon skills
    // 		switch (skill_lv) {
    // 			case 1:
    // 				type = SC_COLORS_OF_HYUN_ROK_1;
    // 				break;
    // 			case 2:
    // 				type = SC_COLORS_OF_HYUN_ROK_2;
    // 				break;
    // 			case 3:
    // 				type = SC_COLORS_OF_HYUN_ROK_3;
    // 				break;
    // 			case 4:
    // 				type = SC_COLORS_OF_HYUN_ROK_4;
    // 				break;
    // 			case 5:
    // 				type = SC_COLORS_OF_HYUN_ROK_5;
    // 				break;
    // 			case 6:
    // 				type = SC_COLORS_OF_HYUN_ROK_6;
    // 				break;
    // 		}
    // 		sc_start(src, target, type, 100, skill_lv, skill_get_time(getSkillId(),skill_lv));
    // 		clif_skill_nodamage(src, *src, getSkillId(), skill_lv);
    // 	}
    }
}
