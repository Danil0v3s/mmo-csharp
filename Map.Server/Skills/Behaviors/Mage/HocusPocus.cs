using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_ABRACADABRA — Sage Hocus-Pocus (Abracadabra). Manual port of
/// <c>rathena-fork/src/map/skills/mage/hocuspocus.cpp</c>.
///
/// <para>Rolls a random skill from <c>abra_db</c> and casts it. The
/// rAthena flow:
/// <list type="bullet">
///   <item>Pick a random row from <c>abra_db</c>.</item>
///   <item>Roll <c>per[lv]</c> (in 10000ths) — re-pick if it fails, up
///         to <c>abra_db.size() * 3</c> attempts.</item>
///   <item>PC casters: set <c>sd.skillitem</c> + <c>sd.skillitemlv</c> and
///         emit <c>clif_item_skill</c> so the next skill use overrides
///         with the picked spell.</item>
///   <item>Mob casters: dispatch via <c>unit_skilluse_id</c> /
///         <c>unit_skilluse_pos</c> directly.</item>
/// </list>
/// We use <see cref="IAbraDatabase.PickRandom"/> for the roll and
/// <see cref="ISkillCastService.ResolveSkill"/> /
/// <see cref="ISkillCastService.ResolveSkillAt"/> for the dispatch.
/// </para>
/// </summary>
public sealed class HocusPocus : SkillImpl
{
    public HocusPocus() : base(SkillIds.SA_ABRACADABRA) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        // rAthena: if (abra_db.empty()) silently emit the cast and return.
        var abra = ctx.Abra;
        if (abra == null || abra.Count == 0) return;

        var picked = abra.PickRandom(Random.Shared);
        if (picked == null || picked.Value == 0) return;
        var pickedId = picked.Value;

        // PC casters: rAthena sets sd->skillitem so the next skill use
        // overrides with the picked spell, then emits clif_item_skill
        // which pops the spell-pick UI. Without the UI packet today we
        // dispatch directly — the player sees an instant cast of the
        // rolled spell, which matches the mob path's behavior.
        // Mob casters: rAthena calls unit_skilluse_* on the current
        // target. ResolveSkill is the C# equivalent and runs the cast
        // instantly via the SkillImpl plugin registry.
        if (ctx.Cast == null) return;
        // We don't have skill_get_casttype access here yet; route both
        // ID and Ground variants — the one whose CastendDamageId /
        // CastendPos2 hook exists wins. ResolveSkill returns false on
        // unknown skills and falls through cleanly.
        if (!ctx.Cast.ResolveSkill(src, target, pickedId, skillLevel))
        {
            ctx.Cast.ResolveSkillAt(src, target.X, target.Y, pickedId, skillLevel);
        }
    }
}
