using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_STYLE_CHANGE — auto-generated stub from
/// <c>src/map/skills/homunculus/homunculus_stylechange.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class StyleChange : SkillImpl
{
    public StyleChange() : base(SkillIds.MH_STYLE_CHANGE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // homun_data* hd = BL_CAST(BL_HOM, src);
    // 
    // 	if (hd) {
    // 		struct status_change_entry* sce;
    // 		if ((sce = hd->sc.getSCE(SC_STYLE_CHANGE)) != nullptr) { //in preparation for other bl usage
    // 			if (sce->val1 == MH_MD_FIGHTING) {
    // 				sce->val1 = MH_MD_GRAPPLING;
    // 			} else {
    // 				sce->val1 = MH_MD_FIGHTING;
    // 			}
    // 			//if(hd->master && hd->sc.getSCE(SC_STYLE_CHANGE)) { // Aegis does not show any message when switching fighting style
    // 			//	char output[128];
    // 			//	safesnprintf(output,sizeof(output),msg_txt(sd,378),(sce->val1==MH_MD_FIGHTING?"fighthing":"grappling"));
    // 			//	clif_messagecolor(hd->master, color_table[COLOR_RED], output, false, SELF);
    // 			//}
    // 		} else {
    // 			sc_start(hd, hd, SC_STYLE_CHANGE, 100, MH_MD_FIGHTING, INFINITE_TICK);
    // 		}
    // 	}
    }
}
