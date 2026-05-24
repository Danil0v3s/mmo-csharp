using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_FORTUNE — Sage Gold Digger (Hocus Pocus pool entry). Grants the
/// caster <c>targetLevel * 100</c> zeny (rAthena skill.cpp:SA_FORTUNE).
/// Zeny lives on <c>MapSessionData.CharacterData</c>; the in-memory
/// mutation flushes to the char-server via the standard PC autosave
/// pipeline.
/// </summary>
public sealed class GoldDigger : SkillImpl
{
    public GoldDigger() : base(SkillIds.SA_FORTUNE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (src is not PlayerEntity pc) return;
        var session = ctx.Sessions?.TryGet(pc);
        if (session?.CharacterData is null) return;
        var add = (ulong)target.Level * 100;
        var next = session.CharacterData.Zeny + add;
        session.CharacterData.Zeny = (uint)Math.Min(uint.MaxValue, next);
    }
}
