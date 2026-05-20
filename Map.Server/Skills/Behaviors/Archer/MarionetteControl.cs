using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// CG_MARIONETTE — auto-generated stub from
/// <c>src/map/skills/archer/marionettecontrol.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class MarionetteControl : SkillImpl
{
    public MarionetteControl() : base(SkillIds.CG_MARIONETTE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data *sd = BL_CAST(BL_PC, src);
    // 	map_session_data *dstsd = BL_CAST(BL_PC, target);
    // 	status_change *sc = status_get_sc(src);
    // 	status_change *tsc = status_get_sc(target);
    // 
    // 	if ((sd && dstsd && (dstsd->class_ & MAPID_SECONDMASK) == MAPID_BARDDANCER && dstsd->status.sex == sd->status.sex) ||
    // 		(tsc && (tsc->getSCE(SC_CURSE) || tsc->getSCE(SC_QUAGMIRE)))) {
    // 		// Cannot cast on another bard/dancer-type class of the same gender as caster, or targets under Curse/Quagmire
    // 		if (sd != nullptr) {
    // 			clif_skill_fail(*sd, getSkillId());
    // 		}
    // 		flag |= SKILL_NOCONSUME_REQ;
    // 		return;
    // 	}
    // 
    // 	if (sc && tsc) {
    // 		if (!sc->getSCE(SC_MARIONETTE) && !tsc->getSCE(SC_MARIONETTE2)) {
    // 			sc_start(src, src, SC_MARIONETTE, 100, target->id, skill_get_time(getSkillId(), skill_lv));
    // 			sc_start(src, target, SC_MARIONETTE2, 100, src->id, skill_get_time(getSkillId(), skill_lv));
    // 			clif_skill_nodamage(src, *target, getSkillId(), skill_lv);
    // 		} else if (sc->getSCE(SC_MARIONETTE) && sc->getSCE(SC_MARIONETTE)->val1 == target->id &&
    // 			tsc->getSCE(SC_MARIONETTE2) && tsc->getSCE(SC_MARIONETTE2)->val1 == src->id) {
    // 			status_change_end(src, SC_MARIONETTE);
    // 			status_change_end(target, SC_MARIONETTE2);
    // 		} else {
    // 			if (sd != nullptr) {
    // 				clif_skill_fail(*sd, getSkillId());
    // 			}
    // 			flag |= SKILL_NOCONSUME_REQ;
    // 			return;
    // 		}
    // 	}
    }
}
