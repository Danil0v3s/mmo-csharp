using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AL_BLESSING — Acolyte Blessing. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/blessing.cpp</c>.
///
/// <para>Structurally identical to <see cref="IncreaseAgi"/> — same
/// undead-conversion damage branch, same 100 % SC application on
/// the buff path. The only differences are the applied SC type
/// (<see cref="StatusType.Blessing"/>) and the duration ladder.</para>
///
/// <para>Duration matches AL_INCAGI: <c>(40 + 20*lv) * 1000</c> ms
/// (60 s at lv 1, 240 s at lv 10).</para>
/// </summary>
public sealed class Blessing : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public Blessing() : base(SkillIds.AL_BLESSING) { }

    public Blessing(ISkillAttackService? skillAttack = null) : base(SkillIds.AL_BLESSING)
    {
        _skillAttack = skillAttack;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: clif_skill_nodamage broadcast happens first, regardless of branch.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        // rAthena undead-conversion branch: SC_CHANGEUNDEAD on a PC target
        // turns Blessing into Misc damage (Holy vs Undead resolution).
        if (target is PlayerEntity && ctx.Sc?.Get(target, StatusType.Changeundead) != null)
        {
            if (target.Stats.Hp > 1)
            {
                _skillAttack?.SkillAttack(BattleAttackType.Misc, src, src, target, SkillId, skillLevel);
            }
            return;
        }

        // rAthena: sc_start(src, bl, type, 100, skill_lv, skill_get_time(getSkillId(), skill_lv));
        var duration = (40 + 20 * skillLevel) * 1000;
        ctx.Sc?.Start(target, StatusType.Blessing, val1: skillLevel, 0, 0, 0, duration, src);
    }
}
