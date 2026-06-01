using Map.Server.Entities;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// PF_HPCONVERSION — Professor Indulge (HP Conversion). Manual port of
/// <c>rathena-fork/src/map/skills/mage/indulge.cpp</c>.
///
/// <para>Converts <c>max_hp / 10</c> HP into <c>(max_hp / 10) * lv</c>
/// SP for the target. Fails (skill-fail packet) if the caster's
/// current HP can't cover the charge.</para>
/// </summary>
public sealed class Indulge : SkillImpl
{
    private readonly IStatusOpsService? _statusOps;

    public Indulge() : base(SkillIds.PF_HPCONVERSION) { }

    public Indulge(IStatusOpsService? statusOps = null) : base(SkillIds.PF_HPCONVERSION)
    {
        _statusOps = statusOps;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var hp = src.Stats.MaxHp / 10;
        var sp = hp * skillLevel;
        // rAthena status_charge: fail if current HP can't cover the cost.
        var currentHp = src switch
        {
            PlayerEntity pc => pc.Hp,
            MobEntity mb => mb.Hp,
            _ => src.Stats.MaxHp,
        };
        if (currentHp <= hp)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.Skill);
            return;
        }
        _statusOps?.Heal(src, -hp, 0, 0);
        _statusOps?.Heal(target, 0, sp, 2);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
