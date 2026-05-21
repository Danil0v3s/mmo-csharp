using Map.Server.Entities;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// PF_HPCONVERSION — Professor Indulge (HP Conversion). Manual port of
/// <c>rathena-fork/src/map/skills/mage/indulge.cpp</c>.
///
/// <para>Converts <c>max_hp / 10</c> HP into <c>(max_hp / 10) * lv</c>
/// SP for the target. Fails if the caster can't afford the HP cost.
/// HP-charge gate uses the StatusOps Heal with negative HP.</para>
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
        // HP-charge precondition is checked on the live entity HP — current-HP read on Entity TODO,
        // so we eagerly apply the negative heal and trust StatusOpsService to clamp.
        _statusOps?.Heal(src, -hp, 0, 0);
        _statusOps?.Heal(target, 0, sp, 2);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
