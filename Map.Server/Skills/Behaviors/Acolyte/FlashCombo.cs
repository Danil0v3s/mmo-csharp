using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_FLASHCOMBO — auto-generated stub from
/// <c>src/map/skills/acolyte/flashcombo.hpp</c>.
///
/// <para>Inherits <see cref="StatusSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class FlashCombo : StatusSkillImpl
{
    public FlashCombo() : base(SkillIds.SR_FLASHCOMBO) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // map_session_data* sd = BL_CAST(BL_PC, src);
    // 
    // 	const int32 combo[] = { SR_DRAGONCOMBO, SR_FALLENEMPIRE, SR_TIGERCANNON };
    // 	const int32 delay[] = { 0, 750, 1250 };
    // 
    // 	if (sd) // Disable attacking/acting/moving for skill's duration.
    // 		sd->ud.attackabletime = sd->canuseitem_tick = sd->ud.canact_tick = tick + delay[2];
    // 
    // 	StatusSkillImpl::castendNoDamageId(src, target, skill_lv, tick, flag);
    // 
    // 	for (int32 i = 0; i < ARRAYLENGTH(combo); i++)
    // 		skill_addtimerskill(src,tick + delay[i],target->id,0,0,combo[i],skill_lv,BF_WEAPON,flag|SD_LEVEL);
    }
}
