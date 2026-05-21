using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// PR_STRECOVERY — Priest Status Recovery. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/statusrecovery.cpp</c>.
///
/// <para>Cures the "body-state" debuff family (Stone / Freeze /
/// Stun / Sleep / Burning / WhiteImprison / StoneWait). Also
/// always ends Netherworld and NoRecover state on the target.</para>
///
/// <para>On undead-race targets: the cure is denied and instead
/// scheduled as a delayed Holy magic hit (1 s later) — Recovery is
/// meant to undamage living things; on undead it damages them.</para>
/// </summary>
public sealed class StatusRecovery : SkillImpl
{
    private readonly Map.Server.Skills.ISkillTimerService? _timers;
    private readonly Map.Server.Skills.ISkillAttackService? _skillAttack;

    public StatusRecovery() : base(SkillIds.PR_STRECOVERY) { }

    public StatusRecovery(
        Map.Server.Skills.ISkillTimerService? timers = null,
        Map.Server.Skills.ISkillAttackService? skillAttack = null) : base(SkillIds.PR_STRECOVERY)
    {
        _timers = timers;
        _skillAttack = skillAttack;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if ((target.Stats.Mode & MobMode.StatusImmune) != 0)
        {
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel, success: false);
            return;
        }

        // Undead target: schedule a 1-second delayed Holy magic attack instead of curing.
        if (target.Stats.Race == BattleRace.Undead || target.Stats.DefenseElement == BattleElement.Undead)
        {
            _timers?.Schedule(src, target, delayMs: 1000, SkillId, skillLevel,
                (s, t, lv) =>
                {
                    _skillAttack?.SkillAttack(BattleAttackType.Magic, s, s, t, SkillId, lv);
                });
        }
        else
        {
            // Body-state cure on living targets.
            ctx.Sc?.End(target, StatusType.Stone);
            ctx.Sc?.End(target, StatusType.Freeze);
            ctx.Sc?.End(target, StatusType.Stun);
            ctx.Sc?.End(target, StatusType.Sleep);
            ctx.Sc?.End(target, StatusType.Burning);
            ctx.Sc?.End(target, StatusType.Whiteimprison);
            ctx.Sc?.End(target, StatusType.Stonewait);
            // TODO: mob_unlocktarget(dstmd, tick) — reset mob AI to idle.
        }

        // Always ends Netherworld + NoRecover state (even on undead).
        ctx.Sc?.End(target, StatusType.Netherworld);
        ctx.Sc?.End(target, StatusType.NorecoverState);

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
