using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_SEVERE_RAINSTORM — Minstrel/Wanderer Severe Rainstorm. Manual
/// port of <c>rathena-fork/src/map/skills/archer/severerainstorm.cpp</c>.
///
/// <para>Drops a damage-trap ground unit. While the skill is active
/// the caster's <see cref="PlayerEntity.CanEquipTick"/> is set so they
/// can't swap equipment during the duration (rAthena <c>sd->canequip_tick</c>).</para>
/// </summary>
public sealed class SevereRainstorm : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public SevereRainstorm() : base(SkillIds.WM_SEVERE_RAINSTORM) { }

    public SevereRainstorm(ISkillUnitService? units = null) : base(SkillIds.WM_SEVERE_RAINSTORM)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: sd->canequip_tick = tick + skill_get_time(getSkillId(), skill_lv)
        if (src is PlayerEntity pc)
        {
            // 4 s/lv duration matches skill_db. Stored as monotonic tick
            // forward — the equip-change gate checks against it.
            pc.CanEquipTick = Environment.TickCount64 + skillLevel * 4000;
        }
        _units?.Place(src, SkillId, skillLevel, x, y);
    }
}
