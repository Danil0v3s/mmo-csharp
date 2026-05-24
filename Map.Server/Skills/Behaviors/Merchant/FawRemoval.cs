using Map.Server.Entities;
using Map.Server.Mob;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_DISJOINT — Mechanic FAW Removal / Disjoint (skill.cpp:NC_DISJOINT
/// arm). Kills a Silver Sniper or Magic Decoy unit belonging to the
/// caster. rAthena gates on the class-id range
/// <see cref="MobIds.SilverSniper"/>..<see cref="MobIds.MagicDecoyWind"/>;
/// any mob outside that range refuses the cast (this prevents the
/// skill from killing arbitrary mobs).
/// </summary>
public sealed class FawRemoval : SkillImpl
{
    public FawRemoval() : base(SkillIds.NC_DISJOINT) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is not MobEntity mob) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (mob.ClassId < MobIds.SilverSniper || mob.ClassId > MobIds.MagicDecoyWind) return;
        if (mob.MasterId != src.Id) return; // own FAW only.
        ctx.Damage?.ApplyDamage(mob, mob.Stats.MaxHp, src);
    }
}
