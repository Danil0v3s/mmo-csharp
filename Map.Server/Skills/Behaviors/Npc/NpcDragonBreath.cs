using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_DRAGONBREATH — Splash dragon breath. lv≤5 fire+burning; lv>5 water+freezing.</summary>
public sealed class NpcDragonBreath : WeaponSkillImpl
{
    public NpcDragonBreath() : base(SkillIds.NPC_DRAGONBREATH) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => skillLevel > 5
            ? baseRatio + 500 + 500 * (skillLevel - 5)
            : baseRatio + 500 + 500 * skillLevel;
    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (System.Random.Shared.Next(100) < 50)
        {
            var sc = skillLevel > 5 ? StatusType.Freezing : StatusType.Burning;
            ctx.Sc?.Start(target, sc, val1: skillLevel, val2: 1000, val3: (int)src.Id.Value, 0, durationMs: 10_000, src);
        }
    }
}
