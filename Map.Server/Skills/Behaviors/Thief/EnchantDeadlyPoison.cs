using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ASC_EDP — Enchant Deadly Poison. Manual port of
/// <c>rathena-fork/src/map/skills/thief/enchantdeadlypoison.cpp</c>.
/// Status-only buff; +25% poison-element pseudo-watk handled by the
/// status-side renewal effect (TODO).
/// </summary>
public sealed class EnchantDeadlyPoison : StatusSkillImpl
{
    public EnchantDeadlyPoison() : base(SkillIds.ASC_EDP) { }
}
