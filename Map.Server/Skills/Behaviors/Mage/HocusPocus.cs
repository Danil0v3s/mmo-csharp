using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_ABRACADABRA — Sage Hocus-Pocus (Abracadabra). Manual port of
/// <c>rathena-fork/src/map/skills/mage/hocuspocus.cpp</c>.
///
/// <para>Rolls a random skill from <c>abra_db.yml</c> and casts it on
/// the target. Probability weights are per-row (<c>per[lv]</c> in
/// 10000ths). The abra DB loader, the spell-pick UI for player casters
/// (<c>clif_item_skill</c>), and the auto-dispatch for non-player
/// casters all need wiring — for now we just broadcast the cast
/// without rolling.</para>
/// </summary>
public sealed class HocusPocus : SkillImpl
{
    public HocusPocus() : base(SkillIds.SA_ABRACADABRA) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // TODO: roll abra_db, set sd.skillitem for players or unit_skilluse_* for mobs.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
