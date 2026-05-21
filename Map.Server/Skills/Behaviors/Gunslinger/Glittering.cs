using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// GS_GLITTERING — Gunslinger Glittering Coin. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/glittering.cpp</c>.
/// (20 + 10*lv)% to gain a coin orb, otherwise lose one. Coin orb
/// system is TODO; we land the animation.
/// </summary>
public sealed class Glittering : SkillImpl
{
    public Glittering() : base(SkillIds.GS_GLITTERING) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // TODO: pc_addspiritball / pc_delspiritball, gated on RL_RICHS_COIN.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
