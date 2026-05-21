using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_S_PHARMACY — Genetic Special Pharmacy. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/specialpharmacy.cpp</c>.
/// Stashes skill_id_old / skill_lv_old and opens cooking list type 29
/// (qty=1, page=6). The cooking dialog UI is not yet wired, so we
/// broadcast the no-damage animation and TODO the cook list packet.
/// </summary>
public sealed class SpecialPharmacy : SkillImpl
{
    public SpecialPharmacy() : base(SkillIds.GN_S_PHARMACY) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // TODO: send clif_cooking_list(sd, type=29, GN_S_PHARMACY, qty=1, page=6) once
        // the production UI is ported. Until then, just animate.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
