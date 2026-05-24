using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_SUMMONBL — Warlock Summon Lightning Ball. Manual port of
/// <c>rathena-fork/src/map/skills/mage/summonlightningball.cpp</c>.
/// Same shape as <see cref="SummonStone"/> — fills an SC_SPHERE_* slot
/// with WLS_WIND.
/// </summary>
public sealed class SummonLightningBall : SkillImpl
{
    public SummonLightningBall() : base(SkillIds.WL_SUMMONBL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Deferred: SC_SPHERE_1..5 slot management (Warlock summoned-ball element registry)
        // isn't wired through IStatusChangeService yet — WLS_WIND ball not retained.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, 0);
    }
}
