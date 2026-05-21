using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// HP_ASSUMPTIO — High Priest Assumptio. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/assumptio.cpp</c>.
///
/// <para>Buff that halves incoming damage. Rejects the cast when a
/// player tries to Assumptio a mob (would buff the enemy's
/// defenses).</para>
///
/// <para>Duration: <c>20 * skillLevel</c> seconds (20 s at lv 1,
/// 100 s at lv 5).</para>
/// </summary>
public sealed class Assumptio : SkillImpl
{
    public Assumptio() : base(SkillIds.HP_ASSUMPTIO) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: if (sd && dstmd) clif_skill_fail(*sd, getSkillId());
        if (src is PlayerEntity caster && target is MobEntity)
        {
            ctx.Client?.BroadcastSkillFail(caster, SkillId,
                Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }

        // Standard buff apply: SC_ASSUMPTIO val1 = level, duration = 20s * lv.
        var duration = 20_000 * skillLevel;
        ctx.Sc?.Start(target, StatusType.Assumptio, val1: skillLevel, 0, 0, 0, duration, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
