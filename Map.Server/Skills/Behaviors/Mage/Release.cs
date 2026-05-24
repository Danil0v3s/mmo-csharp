using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// WL_RELEASE — Warlock Release (skill.cpp:WL_RELEASE arm). Two modes:
/// <list type="bullet">
///   <item><b>lv 1</b> — pops the newest memorized spell from the
///         SC_SPELLBOOK ring (and drains its points off SC_FREEZE_SP)
///         and re-casts it at the target via
///         <see cref="IUnitOpsService.SkillUseId"/>.</item>
///   <item><b>lv 2</b> — detonates every summoned Spirit Ball
///         (SC_SPHERE_1..5) at the target. Each ball ended; the
///         summoned-ball element family carries the per-elem damage
///         on the SCs the skill engine fires via the ball arms.</item>
/// </list>
/// </summary>
public sealed class Release : SkillImpl
{
    public Release() : base(SkillIds.WL_RELEASE) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity sd) return;
        if (skillLevel == 1)
        {
            // Spellbook release. rathena-fork release.cpp returns
            // silently when SC_FREEZE_SP is absent or no slot is
            // populated — no skill-fail emit.
            var popped = WarlockSpellbookHelpers.ConsumeNewest(sd, ctx);
            if (popped == null) return;
            var (spellId, spellLv) = popped.Value;
            ctx.UnitOps?.SkillUseId(src, target.Id, spellId, spellLv);
            if (!WarlockSpellbookHelpers.HasMemorized(sd, ctx))
            {
                ctx.Sc?.End(sd, StatusType.FreezeSp);
            }
            return;
        }
        // lv 2 — detonate summoned spirit balls. rathena-fork iterates
        // SC_SPHERE_5..1, ends each, and dispatches per-element ATK
        // via skill_addtimerskill. Without a typed per-element ATK
        // sub-skill registry we apply the base WL_RELEASE damage once
        // per ball through SkillAttack and emit clif_skill_nodamage once.
        var sphereSlots = new[]
        {
            StatusType.Sphere5, StatusType.Sphere4, StatusType.Sphere3,
            StatusType.Sphere2, StatusType.Sphere1,
        };
        var any = false;
        foreach (var slot in sphereSlots)
        {
            if (ctx.Sc?.Get(sd, slot) == null) continue;
            ctx.Sc?.End(sd, slot);
            ctx.SkillAttack?.SkillAttack(Combat.BattleAttackType.Magic, src, src, target, SkillId, skillLevel);
            any = true;
        }
        if (any) ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, 0);
    }
}
