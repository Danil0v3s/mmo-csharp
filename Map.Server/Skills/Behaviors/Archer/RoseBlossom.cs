using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// TR_ROSEBLOSSOM — Trouvere Rose Blossom. Manual port of
/// <c>rathena-fork/src/map/skills/archer/roseblossom.cpp</c>.
/// Ratio: <c>+(-100 + 200 + 2000*lv) + 3*CON</c>. On-hit SC_ROSEBLOSSOM
/// seed + ends SC_SOUNDBLEND.
/// </summary>
public sealed class RoseBlossom : WeaponSkillImpl
{
    public RoseBlossom() : base(SkillIds.TR_ROSEBLOSSOM) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 200 + 2000 * skillLevel) + 3 * src.Stats.Con;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Roseblossom, val1: skillLevel, val2: (int)src.Id, 0, 0, durationMs: 5000, src);
        ctx.Sc?.End(target, StatusType.Soundblend);
    }
}
