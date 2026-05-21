using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// SM_SELFPROVOKE — Swordsman Self-Provoke. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/selfprovoke.cpp</c>.
/// Same as Provoke but always targets the caster, ignoring boss /
/// undead immunity gates. Mob-aggro retargeting + coma-check follow-up
/// are TODO.
/// </summary>
public sealed class ProvokeSelf : SkillImpl
{
    public ProvokeSelf() : base(SkillIds.SM_SELFPROVOKE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target.Stats.DefenseElement == BattleElement.Undead) return;
        if ((target.Stats.Mode & MobMode.Mvp) != 0) return;
        var durationMs = 30_000 - 1_000 * skillLevel;
        ctx.Sc?.Start(target, StatusType.Provoke, val1: skillLevel, 0, 0, 0, durationMs, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillIds.SM_PROVOKE, skillLevel);
    }
}
