using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_CHAINACTION — Gunslinger Chain Action (NPC use). Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/chainaction.cpp</c>.
/// Multi-hit cosmetic — the actual chain follow-up is driven by the
/// auto-attack hook. We leave Hits at the SkillDef default.
/// </summary>
public sealed class ChainAction : WeaponSkillImpl
{
    public ChainAction() : base(SkillIds.GS_CHAINACTION) { }
}
