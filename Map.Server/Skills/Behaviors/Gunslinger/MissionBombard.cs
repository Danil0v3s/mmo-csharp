using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// NW_MISSION_BOMBARD — Night Watch Mission Bombard. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/missionbombard.cpp</c>.
/// Ratio <c>+(-100 + 800 + 200*lv) + 5*CON</c> (non-altdamage path).
/// Splash dispatch + ground unit are TODO.
/// </summary>
public sealed class MissionBombard : WeaponSkillImpl
{
    private readonly ISkillUnitService? _units;

    public MissionBombard() : base(SkillIds.NW_MISSION_BOMBARD) { }

    public MissionBombard(ISkillUnitService? units = null) : base(SkillIds.NW_MISSION_BOMBARD)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 800 + 200 * skillLevel) + 5 * src.Stats.Con;

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
