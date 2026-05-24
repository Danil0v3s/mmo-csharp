using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_FIRE_BOMB — Elemental Fire Bomb (skill.cpp:EL_FIRE_BOMB arm).
/// Direct hit ratio +400; on the same hit a 30 % splash (5×5) rolls
/// the EL_FIRE_BOMB_ATK sub-skill (+200 ratio) against every enemy in
/// range. Splash victims are routed through ISkillAttackService with
/// the sub-skill id so battle_calc picks up the lighter scaling.
/// </summary>
public sealed class FireBomb : SkillImpl
{
    private const short SplashRadius = 2;
    private readonly Random _rng;
    private readonly ISkillAttackService? _skillAttack;

    public FireBomb() : base(SkillIds.EL_FIRE_BOMB) => _rng = Random.Shared;

    public FireBomb(ISkillAttackService? skillAttack = null, Random? rng = null) : base(SkillIds.EL_FIRE_BOMB)
    {
        _skillAttack = skillAttack;
        _rng = rng ?? Random.Shared;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 400;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
        // 30 % chance to fire the splash sub-skill at every enemy in
        // a 5×5 around the primary target (rAthena EL_FIRE_BOMB arm).
        if (_rng.Next(100) >= 30) return;
        var victims = ctx.Entities.ForEachInRange(target.MapId, target.X, target.Y, SplashRadius,
            EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == src.Id || v.Id == target.Id) continue;
            _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, v, SkillIds.EL_FIRE_BOMB_ATK, skillLevel);
        }
    }
}
