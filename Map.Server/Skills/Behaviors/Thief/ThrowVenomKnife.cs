using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// AS_VENOMKNIFE — Throw Venom Knife. Manual port of
/// <c>rathena-fork/src/map/skills/thief/throwvenomknife.cpp</c>.
/// Renewal: +400 ratio. 100% SC_POISON on hit.
/// </summary>
public sealed class ThrowVenomKnife : WeaponSkillImpl
{
    public ThrowVenomKnife() : base(SkillIds.AS_VENOMKNIFE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 400;

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Sc?.Start(target, StatusType.Poison, val1: skillLevel, val2: (int)src.Id.Value, 0, 0, durationMs: 20_000, src);
}
