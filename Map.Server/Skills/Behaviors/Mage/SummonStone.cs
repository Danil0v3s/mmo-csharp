using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_SUMMONSTONE — Warlock Summon Stone. Manual port of
/// <c>rathena-fork/src/map/skills/mage/summonstone.cpp</c>.
///
/// <para>Summons an Earth Spirit Ball (element WLS_STONE) into a free
/// SC_SPHERE_1..5 slot (lv 1) or replaces all five slots (lv 2+). The
/// SC_SPHERE_* family isn't on our StatusType enum yet — the per-slot
/// ball management is TODO. We broadcast the cast so the client
/// animation still plays.</para>
/// </summary>
public sealed class SummonStone : SkillImpl
{
    public SummonStone() : base(SkillIds.WL_SUMMONSTONE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Deferred: SC_SPHERE_1..5 slot management (Warlock summoned-ball element registry)
        // isn't wired through IStatusChangeService yet — WLS_STONE ball not retained.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, 0);
    }
}
