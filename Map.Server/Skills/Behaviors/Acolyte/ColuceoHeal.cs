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
        var alHealLearned = ctx.PlayerSkill?.CheckSkill(caster, SkillIds.AL_HEAL) ?? 10;
        if (alHealLearned == 0) alHealLearned = 10;
        var baseHeal = (caster.Level + caster.Stats.IntStat) / 5 * 30 * alHealLearned / 10;
        baseHeal = Math.Max(1, baseHeal);

        // rAthena partycount bonus: partycount > 1 → heal += heal * (partycount * 10 / 4) / 100.
        var partyCount = 0;
        if (caster.PartyId > 0 && ctx.PartyMap != null)
        {
            partyCount = ctx.PartyMap.ForEachOnSameMap(caster, _ => { }, includeSelf: true);
        }
        if (partyCount > 1)
        {
            baseHeal += baseHeal * (partyCount * 10 / 4) / 100;
        }

        ApplyHealOne(target, baseHeal, src, ctx);

        // rAthena fan-out to every other same-map partymate at the same magnitude.
        if (caster.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(caster, m =>
            {
                if (m.Id.Value == target.Id.Value) return;
                ApplyHealOne(m, baseHeal, src, ctx);
            }, includeSelf: false);
        }
    }

    private void ApplyHealOne(Entity bl, int baseHeal, Entity src, SkillBehaviorContext ctx)
    {
        if (bl.Stats.Race == BattleRace.Undead ||
            bl.Stats.DefenseElement == BattleElement.Undead ||
            (ctx.Sc?.Get(bl, StatusType.Berserk) != null)) return;

        var heal = baseHeal;
        // Mado-gear / status-immune zero the heal.
        if ((bl.Stats.Mode & MobMode.StatusImmune) != 0) heal = 0;

        ctx.Client?.BroadcastSkillNoDamage(src, bl, SkillId, heal);

        if (ctx.Sc?.Get(bl, StatusType.Akaitsuki) != null && heal != 0) heal = -heal;

        if (heal > 0) _statusOps?.Heal(bl, heal, 0, 0);
        else if (heal < 0) ctx.Damage?.ApplyDamage(bl, -heal, src);
    }
}
