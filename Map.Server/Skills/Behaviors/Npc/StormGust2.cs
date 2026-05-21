using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_STORMGUST2 — Cell-placed Storm Gust variant. Mirrors
/// <c>rathena-fork/src/map/skills/npc/stormgust2.cpp</c>:
/// CastendPos2 places a skill unit; the damage ratio is +200*lv
/// (not +100*lv as the earlier port had); the per-hit applyAdditional
/// rolls SC_FREEZE at 10%/7%/3% for lv 1/2/3+.
/// </summary>
public sealed class StormGust2 : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public StormGust2() : base(SkillIds.NPC_STORMGUST2) { }
    public StormGust2(ISkillUnitService? units = null) : base(SkillIds.NPC_STORMGUST2) { _units = units; }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 200 * skillLevel;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var rate = skillLevel switch { 1 => 10, 2 => 7, _ => 3 };
        if (System.Random.Shared.Next(100) < rate)
            ctx.Sc?.Start(target, Map.Server.Status.StatusType.Freeze, val1: skillLevel, 0, 0, 0, durationMs: 8_000, src);
    }
}
