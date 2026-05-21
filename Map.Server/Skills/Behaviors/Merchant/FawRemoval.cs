using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_DISJOINT — Mechanic FAW Removal (Disjoint). Manual port of
/// <c>rathena-fork/src/map/skills/merchant/fawremoval.cpp</c>.
/// Kills a Silver Sniper or Magic Decoy unit. Mob-id range check TODO.
/// </summary>
public sealed class FawRemoval : SkillImpl
{
    public FawRemoval() : base(SkillIds.NC_DISJOINT) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is not MobEntity) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // TODO: gate by MOBID_SILVERSNIPER..MOBID_MAGICDECOY_WIND, then status_kill.
    }
}
