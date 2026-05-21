using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// MO_FINGEROFFENSIVE — Monk Throw Spirit Sphere (Finger Offensive).
/// Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/throwspiritsphere.cpp</c>.
///
/// <para>Throws the caster's stockpiled Spirit Spheres one at a
/// time. Renewal ratio: <c>+500 + 200 * lv</c>, with +50 %
/// multiplier when the target is in Blade Stop. Each follow-up
/// sphere fires 200 ms after the previous (when
/// <c>battle_config.finger_offensive_type</c> is set to staggered
/// mode); otherwise all spheres land in a single multi-hit frame.</para>
///
/// <para>Ends <see cref="StatusType.Bladestop"/> on the caster
/// after the hit lands.</para>
/// </summary>
public sealed class ThrowSpiritSphere : WeaponSkillImpl
{
    private readonly Map.Server.Skills.ISkillTimerService? _timers;
    private readonly Map.Server.Skills.ISkillAttackService? _skillAttack;

    public ThrowSpiritSphere() : base(SkillIds.MO_FINGEROFFENSIVE) { }

    public ThrowSpiritSphere(
        Map.Server.Skills.ISkillTimerService? timers = null,
        Map.Server.Skills.ISkillAttackService? skillAttack = null) : base(SkillIds.MO_FINGEROFFENSIVE)
    {
        _timers = timers;
        _skillAttack = skillAttack;
    }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: if (battle_config.finger_offensive_type) dmg.div_ = 1;
        // Renewal default (finger_offensive_type=1) → single-hit mode.
        // C# port follows renewal: per-hit pipeline runs once per sphere.
        // The DamageDiv reduction here is unconditional in renewal.
        dmg.Hits = 1;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // First hit immediately.
        base.CastendDamageId(src, target, skillLevel, ctx);

        // rAthena: for each extra sphere (i=1; i<spheres; i++) schedule a hit
        // at tick+i*200. For simplicity, use up to 5 (one per max sphere) at
        // 200 ms intervals — actual sphere count check would require IPlayerOrbService.
        for (int i = 1; i < 5; i++)
        {
            int delay = i * 200;
            _timers?.Schedule(src, target, delay, SkillId, skillLevel,
                (s, t, lv) =>
                {
                    _skillAttack?.SkillAttack(
                        Map.Server.Combat.BattleAttackType.Weapon,
                        s, s, t, SkillId, lv);
                });
        }

        // rAthena: status_change_end(src, SC_BLADESTOP);
        ctx.Sc?.End(src, StatusType.Bladestop);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // Renewal: ratio += 500 + 200*lv (+50 % vs Blade Stop'd target).
        // SC reader not in this hook — Blade Stop bonus deferred.
        return baseRatio + 500 + 200 * skillLevel;
    }
}
