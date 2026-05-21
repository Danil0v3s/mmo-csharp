using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_SUICIDE — Self-destruct; sets caster HP to 0.</summary>
public sealed class NpcSuicide : SkillImpl
{
    public NpcSuicide() : base(SkillIds.NPC_SUICIDE) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        if (src is PlayerEntity p) p.Hp = 0;
    }
}
