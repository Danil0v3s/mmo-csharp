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

    /// <summary>
    /// rAthena <c>clif_arrow_fail(sd, ARROWFAIL_NO_AMMO)</c> (clif.cpp:4217) — sent only
    /// to the caster when a ranged attack/skill can't fire because no (or wrong-type)
    /// projectile is equipped. Renders the "Please equip the proper ammunition first."
    /// message. Distinct from <see cref="BroadcastSkillFail"/> (a different packet,
    /// ZC_ACTION_FAILURE vs ZC_ACK_TOUSESKILL).
    /// </summary>
    void BroadcastArrowFail(PlayerEntity caster,
        Core.Server.Packets.Out.ZC.ArrowFailType type = Core.Server.Packets.Out.ZC.ArrowFailType.NoAmmo);

    /// <summary>
    /// rAthena <c>clif_skillcasting(src, dst, dst_x, dst_y, skill_id, skill_lv,
    /// property, casttime)</c> (clif.cpp:5930). Broadcasts the cast-start
    /// frame to AOI — clients render the casting bar over the caster.
    /// T5.3a wires this into <see cref="ISkillCastService.StartCast"/> /
    /// <see cref="ISkillCastService.StartCastAt"/> when the cast time is
    /// non-zero (instant casts skip the bar).
    /// </summary>
    /// <param name="src">Caster.</param>
    /// <param name="target">Target entity, or null for ground-targeted casts.</param>
    /// <param name="targetX">Ground cell X (when target is null).</param>
    /// <param name="targetY">Ground cell Y (when target is null).</param>
    /// <param name="skillId">Skill id constant.</param>
    /// <param name="skillLevel">Effective skill level.</param>
    /// <param name="castTimeMs">Total cast time in ms (post-fix).</param>
    void BroadcastSkillCasting(Entity src, Entity? target, short targetX, short targetY,
        ushort skillId, ushort skillLevel, int castTimeMs);

    /// <summary>
    /// rAthena <c>clif_skillcastcancel(bl)</c> (clif.cpp:5973). Broadcasts
    /// the cast-cancel frame to AOI — clients clear the casting bar over
    /// <paramref name="caster"/>. Fires when the cast is interrupted by
    /// damage (rAthena <c>battle_check_dmg_reflect</c>), the caster moves
    /// (rAthena <c>skill_cancel_cast</c>), or a SC ends the cast (Stone /
    /// Freeze / Sleep starting mid-cast).
    /// </summary>
    void BroadcastSkillCastCancel(Entity caster);

    /// <summary>
    /// rAthena <c>clif_skill_estimation</c> (clif.cpp ~5980) — emits
    /// the Sense / Estimation result frame to the caster (self-only).
    /// Carries the target mob's class / level / HP / def / element /
    /// per-element damage modifiers. Used by Mage's MG_SENSE and
    /// the Hunter / Sniper Estimation skill chain.
    /// </summary>
    void BroadcastSkillEstimation(PlayerEntity caster, Entity targetMob);

    /// <summary>
    /// rAthena <c>clif_cooking_list</c> (clif.cpp:14076) — opens the
    /// Pharmacy / Cooking / Mado-mechanic produce dialog with the
    /// list of recipes the caster can attempt at the current
    /// produce-type. <paramref name="produceType"/> mirrors
    /// rAthena's <c>e_skill_produce_type</c>.
    /// </summary>
    void BroadcastCookingList(PlayerEntity caster, int produceType, IReadOnlyList<ushort> craftableItemIds);

    /// <summary>
    /// rAthena <c>clif_skill_produce_mix_list</c> — alias of
    /// <see cref="BroadcastCookingList"/> with a Geneticist/Sage
    /// produce-mix context. Same wire packet
    /// (<c>ZC_MAKABLEITEMLIST</c>) — the <paramref name="produceType"/>
    /// drives the dialog title client-side. Used by GC_CREATENEWPOISON
    /// (type 25), AB_DELUGE / AB_RAISE / GN_MAKEBOMB-style entries.
    /// </summary>
    void BroadcastProduceMixList(PlayerEntity caster, int produceType, IReadOnlyList<ushort> craftableItemIds)
        => BroadcastCookingList(caster, produceType, craftableItemIds);

    /// <summary>
    /// rAthena <c>clif_arrow_create_list</c> — opens the Arrow Crafting
    /// picker (AC_MAKINGARROW). Sends every source-item the caster
    /// holds whose entry exists in <c>create_arrow_db</c>. Reuses the
    /// existing <c>ZC_MAKABLEITEMLIST</c> wire packet with the arrow-
    /// craft produce-type so clients render the right title.
    /// </summary>
    void BroadcastArrowCreateList(PlayerEntity caster, IReadOnlyList<ushort> sourceItemIds)
        => BroadcastCookingList(caster, produceType: 26, sourceItemIds);

    /// <summary>
    /// rAthena <c>clif_item_repair_list</c> — opens the Repair Weapon
    /// picker (BS_REPAIRWEAPON). Sends the target player's broken
    /// inventory rows so the caster can pick one. Default emits via
    /// <see cref="BroadcastCookingList"/> with the repair produce-type
    /// (27) until a typed <c>ZC_ACK_ITEMREPAIR</c> wire packet lands.
    /// </summary>
    void BroadcastItemRepairList(PlayerEntity caster, IReadOnlyList<ushort> brokenItemIds)
        => BroadcastCookingList(caster, produceType: 27, brokenItemIds);

    /// <summary>
    /// rAthena <c>clif_item_identify_list</c> — opens the Identify
    /// picker (MC_IDENTIFY). Sends every unidentified inventory item
    /// the caster owns. Default emits via <see cref="BroadcastCookingList"/>
    /// with the identify produce-type (28).
    /// </summary>
    void BroadcastItemIdentifyList(PlayerEntity caster, IReadOnlyList<ushort> unidentifiedItemIds)
        => BroadcastCookingList(caster, produceType: 28, unidentifiedItemIds);

    /// <summary>
    /// rAthena <c>clif_item_refine_list</c> — opens the WS_WEAPONREFINE
    /// picker. Default reuses the cooking-list wire path with produce-
    /// type 30 (refine).
    /// </summary>
    void BroadcastItemRefineList(PlayerEntity caster, IReadOnlyList<ushort> refinableItemIds)
        => BroadcastCookingList(caster, produceType: 30, refinableItemIds);

    /// <summary>
    /// rAthena <c>clif_skill_itemlistwindow</c> — opens the SO Sorcerer
    /// Four-Spirit Analysis item picker. Default reuses the cooking-
    /// list packet with the four-spirit produce-type (31).
    /// </summary>
    void BroadcastItemListWindow(PlayerEntity caster, IReadOnlyList<ushort> selectableItemIds)
        => BroadcastCookingList(caster, produceType: 31, selectableItemIds);

    /// <summary>
    /// rAthena <c>clif_elementalconverter_list</c> — opens the
    /// SA_CREATECON elemental converter picker. Default reuses the
    /// cooking-list packet with produce-type 33.
    /// </summary>
    void BroadcastElementalConverterList(PlayerEntity caster, IReadOnlyList<ushort> resultItemIds)
        => BroadcastCookingList(caster, produceType: 33, resultItemIds);

    /// <summary>
    /// rAthena <c>clif_magicdecoy_list</c> — opens the WS_MAGICDECOY
    /// elemental decoy picker. Default reuses the cooking-list packet
    /// with produce-type 34.
    /// </summary>
    void BroadcastMagicDecoyList(PlayerEntity caster, IReadOnlyList<ushort> resultItemIds)
        => BroadcastCookingList(caster, produceType: 34, resultItemIds);
}
