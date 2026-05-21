using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WZ_SIGHTBLASTER — Wizard Sight Blaster. Manual port of
/// <c>rathena-fork/src/map/skills/mage/sightblaster.cpp</c>.
///
/// <para>Self-buff that auto-attacks anyone who steps next to the
/// caster. Renewal: +500 % MATK on the per-hit ratio. Buff applies
/// SC_SIGHTBLASTER.</para>
/// </summary>
public sealed class SightBlaster : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public SightBlaster() : base(SkillIds.WZ_SIGHTBLASTER) { }

    public SightBlaster(ISkillAttackService? skillAttack = null) : base(SkillIds.WZ_SIGHTBLASTER)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + 500;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Sightblaster, val1: skillLevel, val2: SkillId, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }
}
