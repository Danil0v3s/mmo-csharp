using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_ADVANCE_PROTECTION — Biolo Advance Protection. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/advanceprotection.cpp</c>.
/// Requires target wearing Shadow Gear. Equip check TODO — we hand
/// off to the base StatusSkillImpl which applies the configured SC.
/// </summary>
public sealed class AdvanceProtection : StatusSkillImpl
{
    public AdvanceProtection() : base(SkillIds.BO_ADVANCE_PROTECTION) { }
}
