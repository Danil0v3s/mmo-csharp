using Map.Server.Entities;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// PF_SOULCHANGE — Professor Soul Exhale (Soul Change). Manual port of
/// <c>rathena-fork/src/map/skills/mage/soulexhale.cpp</c>.
///
/// <para>Two paths. Against a mob: the caster gains <c>3 %</c> of own
/// MaxSP, marks the mob's soul_change_flag so it can't be tapped again.
/// Against a player: the two participants swap their current SP
/// (halved on Renewal, modulo SC_EXTREMITYFIST / SC_NORECOVER_STATE
/// edges). The half-and-swap path needs a current-SP read on Entity
/// which isn't exposed here — we approximate via percent heals.</para>
/// </summary>
public sealed class SoulExhale : SkillImpl
{
    private readonly IStatusOpsService? _statusOps;

    public SoulExhale() : base(SkillIds.PF_SOULCHANGE) { }

    public SoulExhale(IStatusOpsService? statusOps = null) : base(SkillIds.PF_SOULCHANGE)
    {
        _statusOps = statusOps;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is MobEntity)
        {
            // Caster gains 3 % of own max SP. soul_change_flag bookkeeping TODO.
            var sp = src.Stats.MaxSp * 3 / 100;
            _statusOps?.Heal(src, 0, sp, 2);
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
            return;
        }
        // Player-vs-player SP swap (halved) — needs live current-SP read; TODO.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
