using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_TAMINGMONSTER — Sage Beastly Hypnosis (skill.cpp:SA_TAMINGMONSTER
/// arm). Opens the pet-catch UI on the caster targeting the named mob
/// via <see cref="IPetOpsService.CatchProcessStart"/> — rAthena's
/// <c>pet_catch_process_start</c>. The actual catch resolution lands
/// when the caster uses the matching taming item from inventory.
/// </summary>
public sealed class BeastlyHypnosis : SkillImpl
{
    public BeastlyHypnosis() : base(SkillIds.SA_TAMINGMONSTER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (src is not PlayerEntity pc) return;
        if (target is not MobEntity mob) return;
        // Pet-catch needs the target's class id — MobEntity exposes it
        // via ClassId. The PetOpsService stages the catch-in-progress
        // state on the master; the per-mob taming-item gate runs when
        // the user actually consumes the taming item.
        ctx.PetOps?.CatchProcessStart(pc, mob.ClassId);
    }
}
