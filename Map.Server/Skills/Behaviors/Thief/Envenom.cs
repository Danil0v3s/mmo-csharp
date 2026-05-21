using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// TF_POISON — Envenom. Manual port of
/// <c>rathena-fork/src/map/skills/thief/envenom.cpp</c>.
/// Weapon hit; applies SC_POISON at <c>4*lv + 10</c>%.
/// </summary>
public sealed class Envenom : WeaponSkillImpl
{
    public Envenom() : base(SkillIds.TF_POISON) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (System.Random.Shared.Next(100) < 4 * skillLevel + 10)
            ctx.Sc?.Start(target, StatusType.Poison, val1: skillLevel, val2: (int)src.Id.Value, 0, 0, durationMs: 20_000, src);
        else if (src is PlayerEntity sd)
            ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
    }
}
