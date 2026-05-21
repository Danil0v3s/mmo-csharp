using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// BO_CREEPER — Biolo Creeper (bionic). Manual port of
/// <c>rathena-fork/src/map/skills/merchant/creeper.cpp</c>.
/// Same shape as <see cref="AbrBattleWarrior"/> — applies SC_BIONIC_CREEPER
/// + spawns the bionic creeper (mob spawn TODO).
/// </summary>
public sealed class Creeper : SkillImpl
{
    public Creeper() : base(SkillIds.BO_CREEPER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.BionicCreeper, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
    }
}
