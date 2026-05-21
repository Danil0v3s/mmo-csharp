using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ABC_FROM_THE_ABYSS — From The Abyss. Manual port of
/// <c>rathena-fork/src/map/skills/thief/fromtheabyss.cpp</c>.
/// Status-only buff; the linked ABC_FROM_THE_ABYSS_ATK splash has
/// ratio <c>+(-100 + 150 + 650*lv) + 5*spl</c> (handled by its own
/// stub).
/// </summary>
public sealed class FromTheAbyss : StatusSkillImpl
{
    public FromTheAbyss() : base(SkillIds.ABC_FROM_THE_ABYSS) { }
}
