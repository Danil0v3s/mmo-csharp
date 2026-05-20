using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// AL_CRUCIS (id 32) — Acolyte Signum Crucis. rAthena
/// <c>skill.cpp:case AL_CRUCIS</c>: AoE around the caster; applies
/// <see cref="StatusType.Signumcrucis"/> on every Undead / Dark
/// element enemy in radius — drops their DEF by <c>lv * 14 - 7</c>%
/// (or <c>10 + lv * 4 + status_lv</c>% in renewal).
///
/// Duration <c>30 * lv</c> seconds. Skill is the bread-and-butter
/// dungeon-clearing debuff for Acolytes vs Geffenia / Glast Heim.
/// </summary>
public sealed class SignumCrucisBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.AL_CRUCIS;

    private const short Radius = 5; // rAthena: skill_get_splash(AL_CRUCIS) = 5.

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return true;

        var debuff = 10 + skillLevel * 4;
        var durationMs = 30_000 * skillLevel;
        var victims = ctx.Entities.ForEachInRange(
            source.MapId, source.X, source.Y, Radius,
            EntityType.Mob | EntityType.Pc);
        foreach (var v in victims)
        {
            if (v.Id == source.Id) continue;
            // Only Undead / Dark element enemies are eligible.
            if (v.Stats.DefenseElement != BattleElement.Undead
                && v.Stats.DefenseElement != BattleElement.Dark)
                continue;
            ctx.Sc.Start(v, StatusType.Signumcrucis, val1: debuff, 0, 0, 0,
                durationMs, source);
        }
        return true;
    }
}
