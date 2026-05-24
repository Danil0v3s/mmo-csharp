using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_CREATENEWPOISON — Create New Poison. Manual port of
/// <c>rathena-fork/src/map/skills/thief/createnewpoison.cpp</c>.
/// Opens the produce-mix list (subtype 25). Production dialog deferred
/// (clif_skill_produce_mix_list packet not yet ported) — animation only.
/// </summary>
public sealed class CreateNewPoison : SkillImpl
{
    public CreateNewPoison() : base(SkillIds.GC_CREATENEWPOISON) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Deferred: clif_skill_produce_mix_list(sd, GC_CREATENEWPOISON, 25) — produce-mix dialog packet not yet wired.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
