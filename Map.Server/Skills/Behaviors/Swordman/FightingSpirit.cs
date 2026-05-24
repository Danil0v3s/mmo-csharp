using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_FIGHTINGSPIRIT — Rune Knight Fighting Spirit (skill.cpp:11280).
/// Val1 = ATK bonus = 7 × <c>RK_RUNEMASTERY</c> level + 70.
/// Val2 = ASPD bonus = 4 × runemastery level.
/// Cast on a Rune Knight party — each member's runemastery is rolled
/// when the song unit fires; we use the caster's own value as a
/// first-slice proxy (rAthena's group-wide roll lives in party-aware
/// status_change_clear / status_set_viewdata paths the song side runs).
/// </summary>
public sealed class FightingSpirit : SkillImpl
{
    public FightingSpirit() : base(SkillIds.RK_FIGHTINGSPIRIT) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var runeLv = (src is PlayerEntity caster)
            ? (ctx.PlayerSkill?.CheckSkill(caster, SkillIds.RK_RUNEMASTERY) ?? 0)
            : 0;
        ctx.Sc?.Start(target, StatusType.Fightingspirit, val1: 70 + 7 * runeLv, val2: 4 * runeLv, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
