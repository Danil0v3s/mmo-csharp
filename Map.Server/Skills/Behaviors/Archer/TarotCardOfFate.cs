using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// CG_TAROTCARD — Clown/Gypsy Tarot Card of Fate. Manual port of
/// <c>rathena-fork/src/map/skills/archer/tarotcardoffate.cpp</c>.
///
/// <para>Rolls a random tarot card effect at <c>8*lv %</c> success.
/// The actual tarot dispatch (<c>skill_tarotcard</c>) isn't ported —
/// 14 distinct card effects (HP/SP zap, freeze, curse, full-heal,
/// strip equip, etc.). For now we land the broadcast frame only when
/// the roll passes.</para>
/// </summary>
public sealed class TarotCardOfFate : SkillImpl
{
    private readonly Random _rng;

    public TarotCardOfFate() : base(SkillIds.CG_TAROTCARD) => _rng = Random.Shared;

    public TarotCardOfFate(Random? rng = null) : base(SkillIds.CG_TAROTCARD) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) >= 8 * skillLevel)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        // Deferred: skill_tarotcard dispatch — 14 distinct card effects (HP/SP zap, freeze, curse, full-heal, strip equip, etc.) require their own port pass.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
