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
        // Deferred: full map_foreachinshootrange splash dispatch — primary-target hit lands
        // via CastendDamageId; LoS-filtered splash iterator isn't on ISkillAttackService.
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }
}
