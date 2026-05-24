using Map.Server.Entities;

namespace Map.Server.Skills;

/// <summary>
/// Cast pre/post-check + resource consume. Canonical entry points for
/// rAthena <c>skill_check_condition_castbegin</c>,
/// <c>skill_check_condition_castend</c>, <c>skill_consume_hpspap</c>,
/// and <c>skill_consume_requirement</c> (skill.cpp:18347, 19417, 4397,
/// 19685).
///
/// The "castbegin" check fires when the cast button is pressed; the
/// "castend" check fires when the cast bar finishes (catches things
/// that became invalid mid-cast, e.g. weapon swap, equip break).
///
/// Resource columns we can model today (HP / SP / AP / Zeny) flow
/// through real `Consume*` calls. The remaining gates — ammo
/// quantity, item-id requirements, weapon-mask checks, state checks
/// (mounted / sit / cart) — are deferred per PARITY-REMAINING.md
/// §P2.2.b on the skill_db `Required*` column reader; they return
/// success and log so the wiring is visible.
/// </summary>
public interface ISkillRequirementService
{
    /// <summary>rAthena <c>skill_check_condition_castbegin</c> — pre-cast gate.</summary>
    bool CheckCondition(PlayerEntity caster, ushort skillId, ushort skillLevel);

    /// <summary>rAthena <c>skill_check_condition_castend</c> — post-cast re-check.</summary>
    bool CheckConditionCastEnd(PlayerEntity caster, ushort skillId, ushort skillLevel);

    /// <summary>rAthena <c>skill_consume_hpspap</c> — apply HP/SP/AP delta.</summary>
    void ConsumeHpSpAp(Entity bl, ushort skillId, int hp, int sp, int ap);

    /// <summary>
    /// rAthena <c>skill_consume_requirement</c> — final consume step.
    /// <paramref name="type"/> mirrors rAthena flags (1=apply pools,
    /// 2=apply items).
    /// </summary>
    void ConsumeRequirement(PlayerEntity caster, ushort skillId, ushort skillLevel, byte type);

    /// <summary>
    /// rAthena <c>skill_check_condition_char_sub</c> — partner /
    /// party-condition helper used by Royal Guard, Sura, Mechanic.
    /// </summary>
    int CheckConditionCharSub(Entity owner, Entity candidate, ushort skillId);

    /// <summary>
    /// rAthena <c>SKILL_NOCONSUME_REQ</c> flag (skill.cpp:18347-ish) —
    /// when a cast that already paid the requirement decides mid-resolve
    /// to refund (PA_GOSPEL recast self-dispel, MG_SAFETYWALL on Land
    /// Protector overlap), this method walks the same requirement table
    /// in reverse and credits HP/SP/AP/zeny/items back to
    /// <paramref name="caster"/>. Default-interface implementation is a
    /// no-op so legacy impls don't have to override; the real flow
    /// re-uses <see cref="ConsumeRequirement"/> with negative deltas.
    /// </summary>
    void RefundRequirement(PlayerEntity caster, ushort skillId, ushort skillLevel) { }
}
