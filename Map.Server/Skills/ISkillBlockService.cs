using Map.Server.Entities;

namespace Map.Server.Skills;

/// <summary>
/// Per-skill block / cooldown helpers. Canonical entry points for
/// rAthena <c>skill_blockpc_start</c>, <c>skill_blockpc_clear</c>,
/// <c>skill_blockhomun_*</c>, <c>skill_blockmerc_*</c>,
/// <c>skill_block_check</c>, <c>skill_disable_check</c>,
/// <c>skill_addtimerskill</c>, and <c>skill_cleartimerskill</c>
/// (skill.cpp).
///
/// <see cref="ISkillCastService"/> already tracks generic cooldowns
/// via its private cooldown map; this service exposes them under
/// the rAthena names + supports the homun / merc variants. The
/// timer-skill family stores deferred-fire skill events (e.g. Storm
/// Gust strike windows) that fire on
/// <see cref="ISkillCastService.Tick"/>.
/// </summary>
public interface ISkillBlockService
{
    /// <summary>rAthena <c>skill_blockpc_start</c> — start per-skill cooldown.</summary>
    void BlockPcStart(PlayerEntity pc, ushort skillId, int durationMs);
    /// <summary>rAthena <c>skill_blockpc_clear</c> — clear all of a PC's cooldowns.</summary>
    void BlockPcClear(PlayerEntity pc);
    /// <summary>rAthena <c>skill_blockhomun_start</c>.</summary>
    void BlockHomunStart(Entity homun, ushort skillId, int durationMs);
    /// <summary>rAthena <c>skill_blockhomun_clear</c>.</summary>
    void BlockHomunClear(Entity homun);
    /// <summary>rAthena <c>skill_blockmerc_start</c>.</summary>
    void BlockMercStart(Entity merc, ushort skillId, int durationMs);
    /// <summary>rAthena <c>skill_blockmerc_clear</c>.</summary>
    void BlockMercClear(Entity merc);

    /// <summary>rAthena <c>skill_block_check</c> — true if the caster is locked out.</summary>
    bool BlockCheck(Entity caster, ushort skillId);

    /// <summary>rAthena <c>skill_disable_check</c> — per-skill toggle gate.</summary>
    bool DisableCheck(Entity caster, ushort skillId);

    /// <summary>rAthena <c>skill_addtimerskill</c> — queue a delayed-fire skill event.</summary>
    void AddTimerSkill(Entity src, long fireAtTick, EntityId targetId, short x, short y, ushort skillId, ushort skillLevel, int flag);
    /// <summary>rAthena <c>skill_cleartimerskill</c> — drop every deferred event for this caster.</summary>
    void ClearTimerSkill(Entity src);

    /// <summary>Tick — fire any deferred events whose moment has come.</summary>
    void Tick(long nowTick);
}
