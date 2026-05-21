using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_PINPOINTATTACK — Royal Guard Pinpoint Attack. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/pinpointattack.cpp</c>.
/// Ratio <c>+(-100 + 100*lv) + 5*AGI</c>. Per-level on-hit effect:
/// lv1 SC_BLEEDING; lv2-5 break helm/shield/armor/weapon — break-equip
/// pipeline isn't ported yet so those are TODO.
/// </summary>
public sealed class PinpointAttack : WeaponSkillImpl
{
    private readonly Random _rng;

    public PinpointAttack() : base(SkillIds.LG_PINPOINTATTACK) => _rng = Random.Shared;

    public PinpointAttack(Random? rng = null) : base(SkillIds.LG_PINPOINTATTACK)
        => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 100 * skillLevel) + 5 * src.Stats.Agi;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var rate = 30 + 5 * skillLevel + (src.Stats.Agi + src.Level) / 10;
        if (skillLevel == 1)
        {
            if (_rng.Next(100) < rate)
                ctx.Sc?.Start(target, StatusType.Bleeding, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: 30_000, src);
        }
        // TODO: lv2-5 — call skill_break_equip(target, EQP_*, rate*100) once the break-equip path lands.
    }
}
