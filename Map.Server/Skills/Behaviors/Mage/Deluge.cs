using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_DELUGE — Sage Deluge. Drops the Deluge element-field unit.
/// Element-field replacement check (Volcano/Deluge/ViolentGale share
/// a slot) is TODO — needs an ISkillUnitService.LocateElementField helper.
/// </summary>
public sealed class Deluge : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public Deluge() : base(SkillIds.SA_DELUGE) { }
    public Deluge(ISkillUnitService? units = null) : base(SkillIds.SA_DELUGE) => _units = units;
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
