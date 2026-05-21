using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_MAGMA_ERUPTION — Mirrors
/// <c>rathena-fork/src/map/skills/npc/npcmagmaeruption.cpp</c>.
/// Two-stage AoE: (1) immediate weapon-splash "slam" with 90% SC_STUN,
/// (2) delayed eruption damage (skill_addtimerskill). The C# port
/// inherits <see cref="WeaponSkillImpl"/> for stage 1; the deferred
/// stage 2 is a TODO awaiting the skill-timer service integration.
/// </summary>
public sealed class NpcMagmaEruption : WeaponSkillImpl
{
    public NpcMagmaEruption() : base(SkillIds.NPC_MAGMA_ERUPTION) { }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (System.Random.Shared.Next(100) < 90)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
    }
}
