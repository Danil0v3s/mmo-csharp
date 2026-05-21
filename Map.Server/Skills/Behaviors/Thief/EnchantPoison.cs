using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// AS_ENCHANTPOISON — Enchant Poison. Manual port of
/// <c>rathena-fork/src/map/skills/thief/enchantpoison.cpp</c>.
/// Applies SC_ENCHANTPOISON; failure surfaces clif_skill_nodamage(false).
/// </summary>
public sealed class EnchantPoison : StatusSkillImpl
{
    public EnchantPoison() : base(SkillIds.AS_ENCHANTPOISON) { }
}
