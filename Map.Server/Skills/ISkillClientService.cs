using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;

namespace Map.Server.Skills;

/// <summary>
/// Central emitter for skill-result packets that come out of the
/// <c>SkillImpl</c> hierarchy. Mirrors the three rAthena <c>clif_*</c>
/// helpers a skill body reaches for at the moment of resolution:
///
/// <list type="bullet">
///   <item><see cref="BroadcastSkillNoDamage"/> ↔ <c>clif_skill_nodamage</c>
///         — heal/buff/status/self-cast frame (no damage number).</item>
///   <item><see cref="BroadcastSkillDamage"/> ↔ <c>clif_skill_damage</c>
///         — offensive skill hit frame with damage + hit count.</item>
///   <item><see cref="BroadcastSkillFail"/> ↔ <c>clif_skill_fail</c>
///         — caster-only rejection feedback ("Not enough SP", etc.).</item>
/// </list>
///
/// <para>Centralizing here keeps every <c>SkillImpl</c> body off the raw
/// packet API and matches the rAthena layering: the per-skill body says
/// "this skill landed on that target with N damage" and the client
/// service worries about which packet ID to use, who's in view, and
/// whether the source is disguised.</para>
/// </summary>
public interface ISkillClientService
{
    /// <summary>
    /// rAthena <c>clif_skill_nodamage(src, dst, skillId, heal_or_level, success)</c>.
    /// Sends <see cref="ZC_USE_SKILL"/> to everyone in AOI of the target.
    /// </summary>
    /// <param name="src">Caster. May be null for environmental casts.</param>
    /// <param name="target">Target the cast resolved on (self for self-buffs).</param>
    /// <param name="skillId">Skill id constant (<see cref="SkillIds"/>).</param>
    /// <param name="healOrLevel">Heal amount for heal-class skills, skill
    /// level for buff/status casts. The packet field is overloaded; rAthena
    /// uses heal for AL_HEAL and friends, level for everything else.</param>
    /// <param name="success">False if the cast was blocked / immune.
    /// Defaults to true. The client renders a "Skill failed!" overlay when
    /// false on a cast that would otherwise have applied.</param>
    void BroadcastSkillNoDamage(Entity? src, Entity target, ushort skillId, int healOrLevel, bool success = true);

    /// <summary>
    /// rAthena <c>clif_skill_damage(src, target, tick, src_amotion, target_amotion,
    /// damage, count, skill_id, skill_lv, action)</c>.
    /// Sends <see cref="ZC_NOTIFY_SKILL"/> to everyone in AOI of the source.
    /// </summary>
    /// <param name="src">Caster (required for offensive skills).</param>
    /// <param name="target">Target receiving the damage.</param>
    /// <param name="skillId">Skill id constant.</param>
    /// <param name="skillLevel">Effective skill level (used by the client to
    /// pick the right visual variant for level-scaled effects).</param>
    /// <param name="damage">Signed damage total (negative for absorb).</param>
    /// <param name="hitCount">Multi-hit divisor (Sonic Blow=8, Storm Gust=3,
    /// Bowling Bash=2 when target is medium+, etc.).</param>
    /// <param name="action">Animation hint — see <see cref="DamageActionType"/>.</param>
    void BroadcastSkillDamage(Entity src, Entity target, ushort skillId, ushort skillLevel,
        long damage, int hitCount = 1, DamageActionType action = DamageActionType.SkillDamage);

    /// <summary>
    /// rAthena <c>clif_skill_fail(sd, skill_id, cause, btype, itemId)</c>.
    /// Sent <b>only to the caster</b>. Renders the localized fail message
    /// on the client (e.g. "Not enough SP", "Target is too far away").
    /// </summary>
    /// <param name="caster">The player who attempted the cast.</param>
    /// <param name="skillId">Skill id constant.</param>
    /// <param name="cause">Why the cast was rejected.</param>
    /// <param name="btype">Skill-type context (default 0). Used for
    /// item-skill rejection to surface the required count.</param>
    /// <param name="itemId">Optional item id the client mentions in the
    /// fail string (e.g. "You need 1 Red Gemstone").</param>
    void BroadcastSkillFail(PlayerEntity caster, ushort skillId, SkillFailCause cause,
        int btype = 0, uint itemId = 0);
}
