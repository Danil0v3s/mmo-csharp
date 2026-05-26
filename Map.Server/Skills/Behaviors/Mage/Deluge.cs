using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_DELUGE — Sage Deluge. Drops the Deluge element-field unit.
/// INFRA-DEFERRED: the Volcano/Deluge/ViolentGale slot overlap (one
/// field per caster, the new drop deletes the prior one) needs an
/// <c>ISkillUnitService.LocateElementField</c> helper that doesn't
/// exist today.
/// </summary>
public sealed class Deluge : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public Deluge() : base(SkillIds.SA_DELUGE) { }
    public Deluge(ISkillUnitService? units = null) : base(SkillIds.SA_DELUGE) => _units = units;
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
