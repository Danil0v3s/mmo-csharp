using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WZ_SIGHTRASHER — Wizard Sight Rasher. Manual port of
/// <c>rathena-fork/src/map/skills/mage/sightrasher.cpp</c>.
///
/// <para>Ends SC_SIGHT on the caster and fires a splash Wind-magic
/// hit. Per-victim ratio: <c>+20*lv</c>. Splash dispatch is TODO; the
/// primary target gets the magic hit.</para>
/// </summary>
public sealed class SightRasher : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public SightRasher() : base(SkillIds.WZ_SIGHTRASHER) { }

    public SightRasher(ISkillAttackService? skillAttack = null) : base(SkillIds.WZ_SIGHTRASHER)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + 20 * skillLevel;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.End(src, StatusType.Sight);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena map_foreachinshootrange(skill_attack_area, src, ...) — splash
        // around the source (NOT the target) with a line-of-sight gate. Our
        // IEntityRegistry.ForEachInRange is the Chebyshev-distance walker; we
        // pair it with IPathService.PathSearchLong (Bresenham wall check) so
        // only victims with clear LoS to the caster eat the hit.
        const short SplashRadius = 7;
        ctx.Sc?.End(src, StatusType.Sight);
        var victims = ctx.Entities.ForEachInRange(src.MapId, src.X, src.Y, SplashRadius,
            EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == src.Id) continue;
            if (ctx.Paths != null && !ctx.Paths.PathSearchLong(src.MapId, src.X, src.Y, v.X, v.Y))
                continue;
            _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, v, SkillId, skillLevel);
        }
    }
}
