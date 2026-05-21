using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_CHEAL — Coluceo Heal. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/coluceoheal.cpp</c>.
///
/// <para>Party-AoE heal. Per-target heal:</para>
/// <code>
///   amount = AL_HEAL_renewal(caster, target, learnedAlHealLevel)
///   if (party_count > 1)  amount += amount * (party_count * 10 / 4) / 100
///   if (target on Mado-gear or status_immune)  amount = 0
///   if (target has SC_AKAITSUKI)  amount = -amount   (heal → damage)
/// </code>
///
/// <para>Undead-race / SC_BERSERK targets are skipped silently.</para>
///
/// <para>Party-iteration TODO — single-target / no-party fallback
/// matches the inner branch faithfully.</para>
/// </summary>
public sealed class ColuceoHeal : SkillImpl
{
    private readonly IStatusOpsService? _statusOps;

    public ColuceoHeal() : base(SkillIds.AB_CHEAL) { }

    public ColuceoHeal(IStatusOpsService? statusOps = null) : base(SkillIds.AB_CHEAL)
    {
        _statusOps = statusOps;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity caster) return;

        // Undead-race / Berserk → silent no-op.
        if (target.Stats.Race == BattleRace.Undead ||
            target.Stats.DefenseElement == BattleElement.Undead ||
            (ctx.Sc?.Get(target, StatusType.Berserk) != null))
        {
            return;
        }

        // rAthena: i = skill_calc_heal(src, target, AL_HEAL, pc_checkskill(sd, AL_HEAL), true);
        var alHealLearned = caster.LearnedSkills.GetValueOrDefault(SkillIds.AL_HEAL);
        if (alHealLearned == 0) alHealLearned = 10;
        var heal = (caster.Level + caster.Stats.IntStat) / 5 * 30 * alHealLearned / 10;
        heal = Math.Max(1, heal);

        // TODO: partycount bonus — needs party member iteration.
        // partycount > 1: heal += heal * (partycount * 10 / 4) / 100.

        // Mado-gear / status-immune zero the heal (rAthena: "in iRO it
        // breaks the healing to members.. [malufett]").
        if ((target.Stats.Mode & MobMode.StatusImmune) != 0) heal = 0;

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, heal);

        if (ctx.Sc?.Get(target, StatusType.Akaitsuki) != null && heal != 0)
            heal = -heal;

        if (heal > 0)
        {
            _statusOps?.Heal(target, heal, 0, 0);
        }
        else if (heal < 0)
        {
            ctx.Damage?.ApplyDamage(target, -heal, src);
        }
    }
}
