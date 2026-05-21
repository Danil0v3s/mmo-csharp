using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Novice;

/// <summary>
/// NV_FIRSTAID — Novice First Aid. Manual port of
/// <c>rathena-fork/src/map/skills/novice/firstaid.cpp</c>.
/// Flat <c>+5 HP</c> heal on the target, animated with skill level 5.
/// </summary>
public sealed class FirstAid : SkillImpl
{
    public FirstAid() : base(SkillIds.NV_FIRSTAID) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, 5);
        if (target is PlayerEntity p)
            p.Hp = Math.Min(p.MaxHp, p.Hp + 5);
        else if (target is MobEntity m)
            m.Hp = Math.Min(m.MaxHp, m.Hp + 5);
    }
}
