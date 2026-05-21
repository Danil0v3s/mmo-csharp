using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// MO_BALKYOUNG — Monk Ki Explosion. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/kiexplosion.cpp</c>.
///
/// <para>Active hit on target + passive splash that knocks back and
/// stuns non-target enemies. Renewal ratio: <c>+700</c>. Stun
/// chance on splash victims: 70 %.</para>
/// </summary>
public sealed class KiExplosion : SkillImpl
{
    private readonly Map.Server.Skills.ISkillAttackService? _skillAttack;
    private readonly Random _rng;

    public KiExplosion() : base(SkillIds.MO_BALKYOUNG) => _rng = Random.Shared;

    public KiExplosion(
        Map.Server.Skills.ISkillAttackService? skillAttack = null,
        Random? rng = null) : base(SkillIds.MO_BALKYOUNG)
    {
        _skillAttack = skillAttack;
        _rng = rng ?? Random.Shared;
    }

    public override void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: dmg.blewcount = 0 (active hit doesn't knock back the target).
        // C# BattleDamage doesn't carry blew-count yet — TODO.
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Active part: skill_attack(BF_WEAPON, ...) on the named target.
        _skillAttack?.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillId, skillLevel);

        // Splash splash: passive knock-back + stun on every other char in range.
        const short splashRange = 3;
        var victims = ctx.Entities.ForEachInRange(target.MapId, target.X, target.Y, splashRange,
            EntityType.Mob | EntityType.Pc)
            .Where(v => v.Id != src.Id && v.Id != target.Id);
        foreach (var v in victims)
        {
            if (_rng.Next(100) < 70)
            {
                ctx.Sc?.Start(v, StatusType.Stun, val1: skillLevel, 0, 0, 0, 3000, src);
            }
        }
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + 700;
    }
}
