using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_PROVOCATION — Mirrors
/// <c>rathena-fork/src/map/skills/npc/provocation.cpp</c>.
/// Mob unlocks its current target (releases aggro) and broadcasts the
/// cast animation. NOT an SC_PROVOKE application — that was a port bug
/// found via the T3.1 rAthena parity sweep.
/// </summary>
public sealed class Provocation : SkillImpl
{
    public Provocation() : base(SkillIds.NPC_PROVOCATION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // mob_unlocktarget(src, tick) — clears the mob's combat target.
        // TODO: wire through IMobAiService when the mob-aggro interface
        // lands; until then this is a no-op aside from the broadcast.
    }
}
