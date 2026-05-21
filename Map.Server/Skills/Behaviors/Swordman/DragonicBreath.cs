using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// DK_DRAGONIC_BREATH — Dragon Knight Dragonic Breath. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/dragonicbreath.cpp</c>.
/// Ratio <c>+(-100 + 50 + 350*lv) + 7*POW</c>; with SC_DRAGONIC_AURA
/// active, bonus <c>+3*POW + lv*0.07 * (MaxHP/4 + MaxSP/2)</c>;
/// otherwise <c>lv*0.05 * (MaxHP/4 + MaxSP/2)</c>.
/// MaxHP / MaxSP live on the entity sub-types — Mob.Hp/MaxHp vs
/// PlayerEntity.MaxHp/MaxSp. Both common-case branches use the
/// resolved MaxHp/MaxSp from <see cref="UnitStats"/>.
/// </summary>
public sealed class DragonicBreath : RecursiveDamageSplashSkillImpl
{
    public DragonicBreath() : base(SkillIds.DK_DRAGONIC_BREATH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 50 + 350 * skillLevel) + 7 * src.Stats.Pow;
        var hpSpBlock = (skillLevel * (src.Stats.MaxHp * 25 / 100) * 7) / 100
                      + (skillLevel * (src.Stats.MaxSp * 50 / 100) * 7) / 100;
        // The aura buff bumps the multiplier from 5/100 to 7/100; without
        // sc plumbing into the ratio path we apply the no-aura formula.
        ratio += (skillLevel * (src.Stats.MaxHp * 25 / 100) * 5) / 100;
        ratio += (skillLevel * (src.Stats.MaxSp * 50 / 100) * 5) / 100;
        // TODO: with SC_DRAGONIC_AURA, add 3*POW + (hpSpBlock - already-added 5/100 block).
        _ = hpSpBlock; // suppress warning until aura branch is wired.
        return ratio;
    }
}
