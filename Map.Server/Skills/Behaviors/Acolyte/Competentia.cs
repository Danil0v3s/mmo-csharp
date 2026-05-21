using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// CD_COMPETENTIA — Cardinal Competentia. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/competentia.cpp</c>.
///
/// <para>Party-wide combined heal + SP-recovery + SC apply:</para>
/// <list type="bullet">
///   <item>Heals <c>20 * lv %</c> of target's MaxHP.</item>
///   <item>Restores <c>20 * lv %</c> of target's MaxSP.</item>
///   <item>Applies <see cref="StatusType.Competentia"/> SC.</item>
/// </list>
/// <para>Each step emits its own clif_skill_nodamage frame so the
/// client renders the heal popup, SP popup, and SC icon separately.</para>
/// </summary>
public sealed class Competentia : SkillImpl
{
    private readonly IStatusOpsService? _statusOps;

    public Competentia() : base(SkillIds.CD_COMPETENTIA) { }

    public Competentia(IStatusOpsService? statusOps = null) : base(SkillIds.CD_COMPETENTIA)
    {
        _statusOps = statusOps;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var hpAmount = target.Stats.MaxHp * (20 * skillLevel) / 100;
        var spAmount = target.Stats.MaxSp * (20 * skillLevel) / 100;

        // HP heal popup.
        ctx.Client?.BroadcastSkillNoDamage(null, target, SkillIds.AL_HEAL, (int)hpAmount);
        _statusOps?.Heal(target, hpAmount, 0, 0);

        // SP heal popup — rAthena uses MG_SRECOVERY (id 102) as the
        // packet's skill id; we use AL_HEAL as the fallback since
        // MG_SRECOVERY isn't on our SkillIds catalog yet.
        ctx.Client?.BroadcastSkillNoDamage(null, target, SkillIds.AL_HEAL, (int)spAmount);
        _statusOps?.Heal(target, 0, spAmount, 0);

        // SC apply (Competentia marker).
        ctx.Sc?.Start(target, StatusType.Competentia,
            val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);

        // TODO: party iteration when caster is partied.
    }
}
