using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ASC_EDP — auto-generated stub from
/// <c>src/map/skills/thief/enchantdeadlypoison.hpp</c>.
///
/// <para>Inherits <see cref="StatusSkillImpl"/>. Method bodies are TODOs
/// with the original C++ body copied as reference comments.
/// Each per-skill formula needs a real port — the auto-generation
/// preserves structure (class name, base, overrides, skill id) but
/// does not translate C++ semantics to C# automatically.</para>
/// </summary>
public sealed class EnchantDeadlyPoison : StatusSkillImpl
{
    public EnchantDeadlyPoison() : base(SkillIds.ASC_EDP) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
    // TODO: port from rathena-fork. Original C++ body:
    // // EDP also give +25% WATK poison pseudo element to user.
    // 	StatusSkillImpl::castendNoDamageId(src, target, skill_lv, tick, flag);
    // 
    // #ifdef RENEWAL
    // 	sc_start4(src, src, SC_SUB_WEAPONPROPERTY, 100, ELE_POISON, 25, getSkillId(), 0, skill_get_time(getSkillId(), skill_lv));
    // #else
    // 	sc_start4(src, src, SC_WATK_ELEMENT, 100, ELE_POISON, 25, 0, 0, skill_get_time(getSkillId(), skill_lv));
    // #endif
    }
}
