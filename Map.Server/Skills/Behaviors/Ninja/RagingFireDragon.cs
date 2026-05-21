using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// NJ_BAKUENRYU — Raging Fire Dragon. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/ragingfiredragon.cpp</c>.
/// Drops a fire-dragon unit at the target's cell. +50 + 150*lv ratio
/// (charm bonus TODO).
/// </summary>
public sealed class RagingFireDragon : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public RagingFireDragon() : base(SkillIds.NJ_BAKUENRYU) { }

    public RagingFireDragon(ISkillUnitService? units = null) : base(SkillIds.NJ_BAKUENRYU)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 50 + 150 * skillLevel;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        _units?.Place(src, SkillId, skillLevel, target.X, target.Y);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
