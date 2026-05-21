using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SJ_BOOKOFCREATINGSTAR — Book Of Creating Star. POS2 unit placement.</summary>
public sealed class BookofCreatingStar : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public BookofCreatingStar() : base(SkillIds.SJ_BOOKOFCREATINGSTAR) { }
    public BookofCreatingStar(ISkillUnitService? units = null) : base(SkillIds.SJ_BOOKOFCREATINGSTAR) { _units = units; }
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
