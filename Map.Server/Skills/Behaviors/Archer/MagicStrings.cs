using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// BA_POEMBRAGI — Bard Magic Strings (Poem of Bragi). Manual port of
/// <c>rathena-fork/src/map/skills/archer/magicstrings.cpp</c>.
/// Drops the song ground unit (legacy path).
/// </summary>
public sealed class MagicStrings : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public MagicStrings() : base(SkillIds.BA_POEMBRAGI) { }

    public MagicStrings(ISkillUnitService? units = null) : base(SkillIds.BA_POEMBRAGI)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
