using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// TR_SOUNDBLEND — Trouvere/Troubadour Sound Blend. Manual port of
/// <c>rathena-fork/src/map/skills/archer/soundblend.cpp</c>.
///
/// <para>Magic hit + SC_SOUNDBLEND application. Ratio:
/// <c>+(-100 + 120*lv) + 5*SPL</c>, doubled when caster has
/// SC_MYSTIC_SYMPHONY (caster SC readback TODO) with an additional
/// 1.5× vs Fish/Demihuman races.</para>
/// </summary>
public sealed class SoundBlend : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public SoundBlend() : base(SkillIds.TR_SOUNDBLEND) { }

    public SoundBlend(ISkillAttackService? skillAttack = null) : base(SkillIds.TR_SOUNDBLEND)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 120 * skillLevel) + 5 * src.Stats.Spl;
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.Soundblend, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: 30_000, src);
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        CastendDamageId(src, target, skillLevel, ctx);
    }
}
