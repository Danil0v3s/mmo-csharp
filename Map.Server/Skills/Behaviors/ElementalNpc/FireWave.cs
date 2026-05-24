using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_FIRE_WAVE — Elemental Fire Wave. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/firewave.cpp</c>.
/// +1100 ratio; 30% splash via EL_FIRE_WAVE_ATK, else direct hit.
/// EL_FIRE_WAVE_ATK splash variant lives in the same source file (+500).
/// </summary>
public sealed class FireWave : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public FireWave() : base(SkillIds.EL_FIRE_WAVE) { }

    public FireWave(ISkillAttackService? skillAttack = null) : base(SkillIds.EL_FIRE_WAVE)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 1100;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        // Deferred: EL_FIRE_WAVE_ATK splash isn't yet a registered SkillId — treat
        // as single direct hit; 30% splash branch awaits skill-id registration.
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }
}
