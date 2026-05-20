using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// CR_GRANDCROSS — auto-generated stub from
/// <c>src/map/skills/swordman/grandcross.hpp</c>.
///
/// <para>Inherits <see cref="SkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class GrandCross : SkillImpl
{
    public GrandCross() : base(SkillIds.CR_GRANDCROSS) { }

    public override void ApplyCounterAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // if (src == target) {
    // 		// Grand Cross on self specifically only triggers "When hit by physical attack" autospells and ignores everything else
    // 		attack_type |= BF_WEAPON;
    // 		attack_type &= ~BF_MAGIC;
    // 	}
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // //Set flag to 1 to prevent deleting ammo (it will be deleted on group-delete).
    // 	flag|=1;
    // 
    // 	skill_unitsetting(src,getSkillId(),skill_lv,x,y,0);
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* dstsd = BL_CAST(BL_PC, target);
    // 	status_data* tstatus = status_get_status_data(*target);
    // 
    // 	//Chance to cause blind status vs demon and undead element, but not against players
    // 	if(!dstsd && (battle_check_undead(tstatus->race,tstatus->def_ele) || tstatus->race == RC_DEMON))
    // 		sc_start(src,target,SC_BLIND,100,skill_lv,skill_get_time2(getSkillId(),skill_lv));
    }
}
