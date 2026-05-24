using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_FORTUNE — Sage Gold Digger (Hocus Pocus). Grants the caster
/// <c>targetLevel * 100</c> zeny. Player-only. Zeny grant is TODO:
/// PlayerEntity doesn't carry the zeny field yet — it lives in the
/// char DB row and changes route through char-server IPC.
/// </summary>
public sealed class GoldDigger : SkillImpl
{
    public GoldDigger() : base(SkillIds.SA_FORTUNE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // Deferred: zeny grant — PlayerEntity has no zeny field; the value lives in the
        // char-server DB row and mutations route through char-server IPC.
    }
}
