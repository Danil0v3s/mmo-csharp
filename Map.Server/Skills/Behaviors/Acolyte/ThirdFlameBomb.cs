using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// IQ_THIRD_FLAME_BOMB — Inquisitor Third Flame Bomb. Manual port
/// of <c>rathena-fork/src/map/skills/acolyte/thirdflamebomb.cpp</c>.
///
/// <para>Holy splash with sphere-driven multi-hit (cap 3). Ratio:
/// <c>-100 + 650*lv + 10*POW + (MaxHP * 20 / 100)</c>. Ends
/// <see cref="StatusType.SecondBrand"/> on hit.</para>
/// </summary>
public sealed class ThirdFlameBomb : RecursiveDamageSplashSkillImpl
{
    public ThirdFlameBomb() : base(SkillIds.IQ_THIRD_FLAME_BOMB) { }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: dmg.div_ = min(div_ + miscflag, 3) — caps at 3 hits.
        dmg.Hits = Math.Min(3, dmg.Hits + 1);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 650 * skillLevel) + 10 * src.Stats.Pow;
        ratio += (int)(src.Stats.MaxHp * 20 / 100);
        return ratio;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.End(target, StatusType.SecondBrand);
    }
}
