using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ST_REJECTSWORD — Counter Instinct. Manual port of
/// <c>rathena-fork/src/map/skills/thief/counterinstinct.cpp</c>.
/// Buff that grants <c>15*lv</c>% chance to apply SC_AUTOCOUNTER on
/// hit. Auto-counter SC not yet exposed — animation only.
/// </summary>
public sealed class CounterInstinct : StatusSkillImpl
{
    public CounterInstinct() : base(SkillIds.ST_REJECTSWORD) { }
}
