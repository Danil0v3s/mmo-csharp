using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_MAKIBISHI — Makibishi. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/makibishi.cpp</c>.
/// Drops <c>lv+2</c> random spike cells around the caster; 10*lv%
/// stun on hit. Ratio <c>+(-100 + 20*lv)</c>.
/// </summary>
public sealed class Makibishi : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public Makibishi() : base(SkillIds.KO_MAKIBISHI) { }

    public Makibishi(ISkillUnitService? units = null) : base(SkillIds.KO_MAKIBISHI)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 20 * skillLevel);

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        for (int i = 0; i < skillLevel + 2; i++)
        {
            var cx = (short)(src.X - 1 + System.Random.Shared.Next(3));
            var cy = (short)(src.Y - 1 + System.Random.Shared.Next(3));
            _units?.Place(src, SkillId, skillLevel, cx, cy);
        }
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (System.Random.Shared.Next(100) < 10 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
    }
}
