using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_HELLS_PLANT — Genetic Hell's Plant. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/hellsplant.cpp</c>.
/// Base StatusSkillImpl applies the configured SC.
/// </summary>
public sealed class HellsPlant : StatusSkillImpl
{
    public HellsPlant() : base(SkillIds.GN_HELLS_PLANT) { }
}
