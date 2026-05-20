using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_CONVENIO — auto-generated stub from
/// <c>src/map/skills/acolyte/convenio.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class Convenio : SkillImpl
{
    public Convenio() : base(SkillIds.AB_CONVENIO) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST( BL_PC, src );
    // 
    // 	if (sd) {
    // 		party_data *p = party_search(sd->status.party_id);
    // 		int32 i = 0, count = 0;
    // 
    // 		// Only usable in party
    // 		if (p == nullptr) {
    // 			clif_skill_fail( *sd, getSkillId() );
    // 			return;
    // 		}
    // 
    // 		// Only usable as party leader.
    // 		ARR_FIND(0, MAX_PARTY, i, p->data[i].sd == sd);
    // 		if (i == MAX_PARTY || !p->party.member[i].leader) {
    // 			clif_skill_fail( *sd, getSkillId() );
    // 			return;
    // 		}
    // 
    // 		// Do the teleport part
    // 		for (i = 0; i < MAX_PARTY; ++i) {
    // 			map_session_data *pl_sd = p->data[i].sd;
    // 
    // 			if (pl_sd == nullptr || pl_sd == sd || pl_sd->status.party_id != p->party.party_id || pc_isdead(pl_sd) ||
    // 				sd->m != pl_sd->m)
    // 				continue;
    // 
    // 			// Respect /call configuration
    // 			if( pl_sd->status.disable_call ){
    // 				continue;
    // 			}
    // 
    // 			if (!(map_getmapflag(sd->m, MF_NOTELEPORT) || map_getmapflag(sd->m, MF_PVP) || map_getmapflag(sd->m, MF_BATTLEGROUND) || map_flag_gvg2(sd->m))) {
    // 				pc_setpos(pl_sd, map_id2index(sd->m), sd->x, sd->y, CLR_TELEPORT);
    // 				count++;
    // 			}
    // 		}
    // 		if (!count)
    // 			clif_skill_fail( *sd, getSkillId() );
    // 	}
    }
}
