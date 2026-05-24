using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_DESPERADO — Gunslinger Desperado (skill.cpp:GS_DESPERADO arm).
/// Base ratio <c>baseRatio + 50*(lv-1)</c>; doubled when the caster
/// has <c>SC_FALLEN_ANGEL</c> active (Rebellion Fallen Angel proc).
/// CastendPos2 drops the splash unit at (x, y).
/// </summary>
public sealed class Desperado : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public Desperado() : base(SkillIds.GS_DESPERADO) { }

    public Desperado(ISkillUnitService? units = null) : base(SkillIds.GS_DESPERADO)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + 50 * (skillLevel - 1);
        if (ctx.Sc?.Get(src, StatusType.FallenAngel) != null) ratio *= 2;
        return ratio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => (ctx.Units ?? _units)?.Place(src, SkillId, skillLevel, x, y);
}
