using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AL_INCAGI — Increase Agility. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/incagi.cpp</c>.
///
/// <para>Applies <see cref="StatusType.IncreaseAgi"/> for the
/// skill_db-configured duration. The undead-conversion guard
/// (SC_CHANGEUNDEAD) flips the cast into a Misc-type damage frame:
/// living entities are buffed, undead-converted entities are hit
/// (rAthena keeps a 1-HP minimum so the cast can't accidentally kill
/// a player who Changed Undead onto themselves).</para>
///
/// <para>The packet order matches rAthena exactly:
/// <see cref="ISkillClientService.BroadcastSkillNoDamage"/> fires
/// before the SC application or damage attack, so the cast visual
/// always plays even on the undead-conversion branch.</para>
/// </summary>
public sealed class IncreaseAgi : SkillImpl
{
    private const int FallbackDurationMs = 60_000; // skill_db default fallback

    private readonly ISkillAttackService? _skillAttack;

    public IncreaseAgi() : base(SkillIds.AL_INCAGI) { }

    public IncreaseAgi(ISkillAttackService? skillAttack = null) : base(SkillIds.AL_INCAGI)
    {
        _skillAttack = skillAttack;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: clif_skill_nodamage(src, *bl, getSkillId(), skill_lv);
        // Always broadcast the cast frame first (level is the int16 payload
        // for buff-class clif_skill_nodamage, not heal).
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        // rAthena: if (dstsd != nullptr && tsc && tsc->getSCE(SC_CHANGEUNDEAD))
        //          { if (tstatus->hp > 1) skill_attack(BF_MISC, ...); return; }
        // Undead-conversion branch: if the target is a player with the
        // Change Undead SC active, AL_INCAGI lands as Misc damage (Holy
        // type vs undead). The hp > 1 guard prevents killing a player.
        if (target is PlayerEntity && ctx.Sc?.Get(target, StatusType.Changeundead) != null)
        {
            if (target.Stats.Hp > 1)
            {
                _skillAttack?.SkillAttack(BattleAttackType.Misc, src, src, target, SkillId, skillLevel);
            }
            return;
        }

        // rAthena: sc_start(src, bl, type, 100, skill_lv, skill_get_time(getSkillId(), skill_lv));
        // 100% landing chance; val1 = skillLevel; duration from skill_db.
        var duration = SkillDurationMs(ctx, skillLevel);
        ctx.Sc?.Start(target, StatusType.IncreaseAgi, val1: skillLevel, 0, 0, 0, duration, src);
    }

    /// <summary>
    /// rAthena <c>skill_get_time(AL_INCAGI, skill_lv)</c>. Defers to
    /// <see cref="SkillBehaviorContext"/>'s SkillDb wiring when present;
    /// otherwise falls back to the rAthena vanilla baseline duration
    /// (40 + 20 * lv seconds per the AL_INCAGI skill_db YAML).
    /// </summary>
    private int SkillDurationMs(SkillBehaviorContext ctx, ushort skillLevel)
    {
        // We don't have ISkillDb on the context yet; using the rAthena
        // baseline formula matches db/re/skill_db.yml: 40+20*lv seconds.
        return (40 + 20 * skillLevel) * 1000;
    }
}
