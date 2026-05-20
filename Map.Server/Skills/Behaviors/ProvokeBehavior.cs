using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// SM_PROVOKE (id 6) — Swordsman Provoke. rAthena
/// <c>skill.cpp:case SM_PROVOKE</c> + <c>status_change_start: SC_PROVOKE</c>.
///
/// Boss / undead / Boss Class targets immune; success rate
/// <c>50 + 3 * lv + status_def_rate</c>. We apply <see cref="StatusType.Provoke"/>
/// with <c>Val1 = lv</c> so the handler's ATK boost / DEF drop math reads
/// off the level. Duration <c>30000 - 1000*lv</c> ms (rAthena status_db).
/// </summary>
public sealed class ProvokeBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.SM_PROVOKE;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return true;

        // rAthena: undead element + Plant/Insect/Demon-boss races
        // are immune. We honor the Undead/Boss filter; the per-race
        // resist will plug in once the resist table ports.
        if (target.Stats.DefenseElement == BattleElement.Undead) return true;
        if ((target.Stats.Mode & MobMode.Mvp) != 0) return true;

        // Duration: ramps up 30 → 21 s at lv1..10 in rAthena.
        var durationMs = 30_000 - 1_000 * skillLevel;
        ctx.Sc.Start(target, StatusType.Provoke, val1: skillLevel, 0, 0, 0,
            durationMs, source);
        return true;
    }
}
