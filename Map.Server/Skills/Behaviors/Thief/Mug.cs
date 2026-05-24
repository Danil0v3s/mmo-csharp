using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// RG_STEALCOIN — Mug (skill.cpp:RG_STEALCOIN arm). rAthena rate
/// formula: <c>10*lv + dex/2 + luk/2 + 2*(baseLv - targetLv)</c> per
/// 1000. On success: 0 damage, mob-class &gt; 0 gate, zeny credited to
/// the caster's session. Per-mob steal-coin tracking flips the mob's
/// stolen flag so the same mob can't be milked.
/// </summary>
public sealed class Mug : SkillImpl
{
    private readonly Random _rng;

    public Mug() : base(SkillIds.RG_STEALCOIN) => _rng = Random.Shared;

    public Mug(Random? rng = null) : base(SkillIds.RG_STEALCOIN) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (src is not PlayerEntity pc) return;
        if (target is not MobEntity mob) return;
        // rAthena: status-immune (boss) mobs always fail; already-mugged
        // mobs also fail. The MobEntity.StolenCoin flag tracks the per-
        // instance lock-out.
        if ((mob.Stats.Mode & MobMode.StatusImmune) != 0) return;
        if (mob.StolenCoin) return;
        var rate = 10 * skillLevel
                 + pc.Stats.Dex / 2
                 + pc.Stats.Luk / 2
                 + 2 * (pc.Level - target.Level);
        if (rate <= 0) return;
        if (_rng.Next(1000) >= rate) return;
        mob.StolenCoin = true;
        // Zeny gained scales with mob level (rAthena: rand(min, max) where
        // min = mob_lv * lv / 4 and max = mob_lv * lv).
        var max = Math.Max(1, target.Level * skillLevel);
        var zenyGained = (uint)_rng.Next(max / 4 + 1, max + 1);
        var session = ctx.Sessions?.TryGet(pc);
        if (session?.CharacterData == null) return;
        session.CharacterData.Zeny = (uint)Math.Min(uint.MaxValue, session.CharacterData.Zeny + zenyGained);
    }
}
