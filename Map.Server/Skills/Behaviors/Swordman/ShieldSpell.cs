using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_SHIELDSPELL — Royal Guard Shield Spell. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/shieldspell.cpp</c>.
/// Lv1 → SC_SHIELDSPELL_HP, lv2 → SC_SHIELDSPELL_SP, lv3+ → SC_SHIELDSPELL_ATK.
/// </summary>
public sealed class ShieldSpell : SkillImpl
{
    public ShieldSpell() : base(SkillIds.LG_SHIELDSPELL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var type = skillLevel switch
        {
            1 => StatusType.ShieldspellHp,
            2 => StatusType.ShieldspellSp,
            _ => StatusType.ShieldspellAtk,
        };
        ctx.Sc?.Start(target, type, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
