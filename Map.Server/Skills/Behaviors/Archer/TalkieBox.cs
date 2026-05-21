using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// HT_TALKIEBOX — Hunter Talkie Box. Manual port of
/// <c>rathena-fork/src/map/skills/archer/talkiebox.cpp</c>.
/// Drops the talkie-box trap at the cast XY.
/// </summary>
public sealed class TalkieBox : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public TalkieBox() : base(SkillIds.HT_TALKIEBOX) { }

    public TalkieBox(ISkillUnitService? units = null) : base(SkillIds.HT_TALKIEBOX)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
