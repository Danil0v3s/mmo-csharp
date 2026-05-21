using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// BD_DRUMBATTLEFIELD — Bard Battle Theme (Drum on the Battlefield).
/// Manual port of <c>rathena-fork/src/map/skills/archer/battletheme.cpp</c>.
/// Drops the song unit (legacy path) or hands off to the song dispatcher
/// (renewal); dispatcher not wired yet.
/// </summary>
public sealed class BattleTheme : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public BattleTheme() : base(SkillIds.BD_DRUMBATTLEFIELD) { }

    public BattleTheme(ISkillUnitService? units = null) : base(SkillIds.BD_DRUMBATTLEFIELD)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
