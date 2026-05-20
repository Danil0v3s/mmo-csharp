using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Blacksmith;

/// <summary>
/// BS_ADRENALINE — Blacksmith Adrenaline Rush. Mirrors
/// <c>rathena-fork/src/map/skills/blacksmith/adrenalinerush.cpp</c>.
///
/// Apply <see cref="StatusType.Adrenaline"/> on the caster
/// (+30 % ASPD renewal). Party-broadcast pending. Duration
/// <c>60 + 60*(lv-1)</c>s.
/// </summary>
public sealed class AdrenalineRush : SkillImpl
{
    public AdrenalineRush() : base(SkillIds.BS_ADRENALINE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(src, StatusType.Adrenaline, val1: 30, 0, 0, 0,
            durationMs: 60_000 + 60_000 * (skillLevel - 1), src);
    }
}
