using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_CHEAL — Splash heal for all friendly mobs. Splash iteration TODO.</summary>
public sealed class NpcColuceoHeal : SkillImpl
{
    public NpcColuceoHeal() : base(SkillIds.NPC_CHEAL) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (target is PlayerEntity p)
            p.Hp = System.Math.Min(p.MaxHp, p.Hp + p.MaxHp * skillLevel / 10);
    }
}
