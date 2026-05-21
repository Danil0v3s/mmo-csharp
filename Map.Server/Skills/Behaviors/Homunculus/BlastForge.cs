using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Homunculus;

/// <summary>
/// MH_BLAST_FORGE — Homunculus Blast Forge. Manual port of
/// <c>rathena-fork/src/map/skills/homunculus/homunculus_blastforge.cpp</c>.
/// Ratio <c>+(-100 + 70*lv*BaseLv/100) + STR</c>. Drops ground unit at
/// (x, y).
/// </summary>
public sealed class BlastForge : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public BlastForge() : base(SkillIds.MH_BLAST_FORGE) { }

    public BlastForge(ISkillUnitService? units = null) : base(SkillIds.MH_BLAST_FORGE)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 70 * skillLevel * src.Level / 100) + src.Stats.Str;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
