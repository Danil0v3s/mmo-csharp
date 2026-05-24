using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_TYPOON_MIS — Elemental Typhoon Missile. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/typhoonmissile.cpp</c>.
/// +900 ratio; 30% splash, else single hit. Applies SC_SILENCE at
/// 10*lv%. EL_TYPOON_MIS_ATK splash variant lives in the same source
/// file (+1100).
/// </summary>
public sealed class TyphoonMissile : SkillImpl
{
    private const short SplashRadius = 2;
    private readonly Random _rng;
    private readonly ISkillAttackService? _skillAttack;

    public TyphoonMissile() : base(SkillIds.EL_TYPOON_MIS) => _rng = Random.Shared;

    public TyphoonMissile(ISkillAttackService? skillAttack = null, Random? rng = null) : base(SkillIds.EL_TYPOON_MIS)
    {
        _skillAttack = skillAttack;
        _rng = rng ?? Random.Shared;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 900;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
        if (_rng.Next(100) >= 30) return;
        var victims = ctx.Entities.ForEachInRange(target.MapId, target.X, target.Y, SplashRadius,
            EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == src.Id || v.Id == target.Id) continue;
            _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, v, SkillIds.EL_TYPOON_MIS_ATK, skillLevel);
        }
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 10 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Silence, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
    }
}
