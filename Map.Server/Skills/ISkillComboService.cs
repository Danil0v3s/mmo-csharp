using Map.Server.Entities;

namespace Map.Server.Skills;

/// <summary>
/// Combo / partner / banding helpers. Canonical entry points for
/// rAthena <c>skill_combo</c>, <c>skill_is_combo</c>,
/// <c>skill_combo_toggle_inf</c>, <c>skill_check_pc_partner</c>, and
/// <c>skill_banding_count</c> (skill.cpp).
///
/// Combo chains live on PlayerEntity as a "next-allowed-skill" hint
/// that the cast service consults — when set, only the listed
/// follow-up skills can land within the chain window. Partner /
/// banding helpers count nearby allied PCs for Royal Guard / Sura
/// / Rebellion combo skills.
/// </summary>
public interface ISkillComboService
{
    /// <summary>rAthena <c>skill_combo</c> — start the combo window for the next-allowed skill.</summary>
    void Combo(PlayerEntity caster, ushort skillId, ushort skillLevel, int durationMs);

    /// <summary>rAthena <c>skill_is_combo</c> — true if this skill is the in-flight combo follow-up.</summary>
    bool IsCombo(PlayerEntity caster, ushort skillId);

    /// <summary>rAthena <c>skill_combo_toggle_inf</c> — flip the SkillTargetMode bit during a combo.</summary>
    void ComboToggleInf(PlayerEntity caster, ushort skillId, bool combatTargeting);

    /// <summary>rAthena <c>skill_check_pc_partner</c> — count allied PCs within range.</summary>
    int CheckPcPartner(PlayerEntity caster, ushort skillId, ushort skillLevel, short range, bool consumeOnHit);

    /// <summary>rAthena <c>skill_banding_count</c> — Royal Guard Banding members count.</summary>
    int BandingCount(PlayerEntity caster);
}
