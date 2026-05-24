using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_STONE_RAIN — Elemental Stone Rain. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/stonerain.cpp</c>.
/// +200 ratio; 30% splash, else single hit. Splash branch not yet
/// implemented (no skill_area_sub plumbing in elementalNPC layer).
/// </summary>
public sealed class StoneRain : SkillImpl
{
    private const short SplashRadius = 2;
    private readonly Random _rng;
    private readonly ISkillAttackService? _skillAttack;

    public StoneRain() : base(SkillIds.EL_STONE_RAIN) => _rng = Random.Shared;

    public StoneRain(ISkillAttackService? skillAttack = null, Random? rng = null) : base(SkillIds.EL_STONE_RAIN)
    {
        _skillAttack = skillAttack;
        _rng = rng ?? Random.Shared;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 200;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
        // EL_STONE_RAIN has no _ATK variant in rAthena skill_db — the
        // 30 % splash hits with the same skill id (matching rAthena's
        // skill_area_sub broadcast pattern).
        if (_rng.Next(100) >= 30) return;
        var victims = ctx.Entities.ForEachInRange(target.MapId, target.X, target.Y, SplashRadius,
            EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == src.Id || v.Id == target.Id) continue;
            _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, v, SkillId, skillLevel);
        }
    }
}
