using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// PR_ASPERSIO — Priest Aspersio. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/aspersio.cpp</c>.
///
/// <para>Endows the target's weapon with Holy element.</para>
///
/// <list type="bullet">
///   <item><see cref="CastendNoDamageId"/>: applies
///         <see cref="StatusType.Aspersio"/> to friendly targets.
///         When the caster is a player and target is a mob, the
///         cast is suppressed (broadcast carries <c>success=false</c>) —
///         rAthena prevents PCs from accidentally endowing enemies.</item>
///   <item><see cref="CastendDamageId"/>: holy magic attack (the
///         Aspersio skill is also castable on undead, where it
///         resolves as damage instead of buff).</item>
/// </list>
///
/// <para>Duration: <c>(30 + 30 * skillLevel) * 1000</c> ms
/// (60 s at lv 1, 330 s at lv 10).</para>
/// </summary>
public sealed class Aspersio : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public Aspersio() : base(SkillIds.PR_ASPERSIO) { }

    public Aspersio(ISkillAttackService? skillAttack = null) : base(SkillIds.PR_ASPERSIO)
    {
        _skillAttack = skillAttack;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: if (sd && dstmd) { clif_skill_nodamage(..., false); return; }
        // PC caster + mob target → suppress the buff (you can't endow
        // an enemy's weapon). Emit the failed-cast frame.
        if (src is PlayerEntity && target is MobEntity)
        {
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel, success: false);
            return;
        }

        // rAthena: clif_skill_nodamage(src, *target, getSkillId(), skill_lv, sc_start(...));
        var duration = (30 + 30 * skillLevel) * 1000;
        bool landed = ctx.Sc?.Start(target, StatusType.Aspersio,
            val1: skillLevel, 0, 0, 0, duration, src) != null;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel, landed);
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: skill_attack(BF_MAGIC, src, src, target, getSkillId(), skill_lv, tick, flag);
        // Used when the cast lands on undead — Holy element magic.
        _skillAttack?.SkillAttack(BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
    }
}
