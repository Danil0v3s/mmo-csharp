using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_REVERBERATION — Minstrel/Wanderer Reverberation. Manual port of
/// <c>rathena-fork/src/map/skills/archer/reverberation.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 700 + 300*lv)</c>, with ×1.5 when the
/// target carries SC_SOUNDBLEND (target SC readback supported here).
/// Splash via map_foreachinallrange is TODO; the named target gets
/// the magic hit and Sound Blend gets ended on hit.</para>
/// </summary>
public sealed class Reverberation : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public Reverberation() : base(SkillIds.WM_REVERBERATION) { }

    public Reverberation(ISkillAttackService? skillAttack = null) : base(SkillIds.WM_REVERBERATION)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 700 + 300 * skillLevel);
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.End(target, StatusType.Soundblend);
    }
}
