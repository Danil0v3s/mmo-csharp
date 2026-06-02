using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_PINPOINTATTACK — Royal Guard Pinpoint Attack. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/pinpointattack.cpp</c>.
/// Ratio <c>+(-100 + 100*lv) + 5*AGI</c> with the RE_LVL_DMOD(120) base-level
/// scalar (battle.cpp:5401). Per-level on-hit effect: lv1 SC_BLEEDING; lv2-5
/// break helm/shield/armor/weapon via <c>ctx.SideEffect.BreakEquip</c>.
/// </summary>
public sealed class PinpointAttack : WeaponSkillImpl
{
    private readonly Random _rng;

    public PinpointAttack() : base(SkillIds.LG_PINPOINTATTACK) => _rng = Random.Shared;

    public PinpointAttack(Random? rng = null) : base(SkillIds.LG_PINPOINTATTACK)
        => _rng = rng ?? Random.Shared;

    // COMBAT-35 — RE_LVL_DMOD(120) (battle.cpp:5401). ComputeSkillDamage applies
    // this divisor to the ratio above base level 99.
    protected override int ReLvlDivisor => 120;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 100 * skillLevel) + 5 * src.Stats.Agi;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var rate = 30 + 5 * skillLevel + (src.Stats.Agi + src.Level) / 10;
        // rAthena (LG_PINPOINTATTACK): lv1 bleeds, lv2-5 break helm / shield /
        // armor / weapon. The break-equip pipe takes rate as a centi-percent.
        switch (skillLevel)
        {
            case 1:
                if (_rng.Next(100) < rate)
                    ctx.Sc?.Start(target, StatusType.Bleeding, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: 30_000, src);
                break;
            case 2:
                ctx.SideEffect?.BreakEquip(src, target, (int)EquipBits.Helm, rate * 100);
                break;
            case 3:
                ctx.SideEffect?.BreakEquip(src, target, (int)EquipBits.HandL, rate * 100);
                break;
            case 4:
                ctx.SideEffect?.BreakEquip(src, target, (int)EquipBits.Armor, rate * 100);
                break;
            case 5:
                ctx.SideEffect?.BreakEquip(src, target, (int)EquipBits.HandR, rate * 100);
                break;
        }
    }
}
