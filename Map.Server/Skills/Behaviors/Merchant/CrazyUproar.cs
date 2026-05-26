using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MC_LOUD — Merchant Crazy Uproar (Shout). Manual port of
/// <c>rathena-fork/src/map/skills/merchant/crazyuproar.cpp</c>.
/// Renewal: when the caster is partied, the buff splashes via
/// <see cref="Map.Server.Party.IPartyMapService"/> to every same-map
/// member. Solo casters fall through to the base <see cref="StatusSkillImpl"/>
/// which applies the configured SC.
/// </summary>
public sealed class CrazyUproar : StatusSkillImpl
{
    public CrazyUproar() : base(SkillIds.MC_LOUD) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pcSrc || pcSrc.PartyId <= 0 || ctx.PartyMap == null)
        {
            base.CastendNoDamageId(src, target, skillLevel, ctx);
            return;
        }
        ctx.PartyMap.ForEachOnSameMap(pcSrc,
            m => base.CastendNoDamageId(src, m, skillLevel, ctx), includeSelf: true);
    }
}
