using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_ALLHEAL — Mob full HP heal. Heal apply TODO.</summary>
public sealed class FullHeal : SkillImpl
{
    public FullHeal() : base(SkillIds.NPC_ALLHEAL) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is PlayerEntity p) p.Hp = p.MaxHp;
        else if (target is MobEntity m) m.Hp = m.MaxHp;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
