using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MC_LOUD — Merchant Crazy Uproar (Shout). Manual port of
/// <c>rathena-fork/src/map/skills/merchant/crazyuproar.cpp</c>.
/// Party-wide STR buff; party splash via party_foreachsamemap TODO.
/// Base StatusSkillImpl applies the configured SC.
/// </summary>
public sealed class CrazyUproar : StatusSkillImpl
{
    public CrazyUproar() : base(SkillIds.MC_LOUD) { }
}
