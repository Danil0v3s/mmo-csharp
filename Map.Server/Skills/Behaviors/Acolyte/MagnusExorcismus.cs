using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// PR_MAGNUS — Priest Magnus Exorcismus. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/magnusexorcismus.cpp</c>.
/// Drops the ground unit; +30 ratio against Undead/Demon races and
/// Undead-element targets.
/// </summary>
public sealed class MagnusExorcismus : SkillImpl
{
    private readonly Map.Server.Skills.ISkillUnitService? _units;

    public MagnusExorcismus() : base(SkillIds.PR_MAGNUS) { }

    public MagnusExorcismus(Map.Server.Skills.ISkillUnitService? units = null) : base(SkillIds.PR_MAGNUS)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: skill_unitsetting(src, getSkillId(), skill_lv, x, y, 0);
        // Drops the Magnus Exorcismus ground unit; per-tick damage to
        // Undead/Demon races inside the splash is handled by the
        // SkillUnit engine via skill_unit_db entry.
        _units?.Place(src, SkillId, skillLevel, x, y);
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: if (undead-race or undead-element or demon-race) baseRatio += 30;
        if (target.Stats.Race == BattleRace.Undead ||
            target.Stats.DefenseElement == BattleElement.Undead ||
            target.Stats.Race == BattleRace.Demon)
        {
            return baseRatio + 30;
        }
        return baseRatio;
    }
}
