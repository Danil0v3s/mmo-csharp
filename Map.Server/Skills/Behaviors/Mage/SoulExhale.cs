using Map.Server.Entities;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// PF_SOULCHANGE — Professor Soul Exhale (Soul Change). Manual port of
/// <c>rathena-fork/src/map/skills/mage/soulexhale.cpp</c>.
///
/// <para>Two paths. Against a mob: the caster gains <c>3 %</c> of own
/// MaxSP. Against a player: the two participants swap their current
/// SP (halved on Renewal).</para>
///
/// <para>INFRA-DEFERRED: the mob's <c>soul_change_flag</c> per-mob
/// bookkeeping (one tap per mob lifetime) needs a flag on
/// <see cref="MobEntity"/> that doesn't exist today; without it the
/// skill can be repeatedly tapped on the same mob.</para>
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
            // Caster gains 3 % of own max SP.
            var sp = src.Stats.MaxSp * 3 / 100;
            _statusOps?.Heal(src, 0, sp, 2);
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
            return;
        }
        // Player-vs-player SP swap (halved on Renewal).
        if (src is PlayerEntity srcPc && target is PlayerEntity dstPc)
        {
            var srcSp = srcPc.Sp / 2;
            var dstSp = dstPc.Sp / 2;
            // Drain each side's current half, then heal the other's half across.
            _statusOps?.Heal(srcPc, 0, -srcSp, 0);
            _statusOps?.Heal(dstPc, 0, -dstSp, 0);
            _statusOps?.Heal(srcPc, 0, dstSp, 2);
            _statusOps?.Heal(dstPc, 0, srcSp, 2);
        }
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
