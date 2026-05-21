using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_GENTLETOUCH_CURE — Sura Gentle Touch (Cure). Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/gentletouchcure.cpp</c>.
///
/// <para>Heals <c>(120 * lv) + (MaxHP * lv / 100)</c> and chance-
/// cures status effects: Stone, Freeze, Stun, Poison, Silence,
/// Blind, Hallucination. Cure-chance formula:
/// <c>(lv*5 + (DEX + casterLv) / 4) - rnd(1..10)</c>.</para>
///
/// <para>Emperium / Battlefield-class mobs heal for 0.</para>
/// </summary>
public sealed class GentleTouchCure : SkillImpl
{
    private readonly IStatusOpsService? _statusOps;
    private readonly Random _rng;

    public GentleTouchCure() : base(SkillIds.SR_GENTLETOUCH_CURE) => _rng = Random.Shared;

    public GentleTouchCure(IStatusOpsService? statusOps = null, Random? rng = null) : base(SkillIds.SR_GENTLETOUCH_CURE)
    {
        _statusOps = statusOps;
        _rng = rng ?? Random.Shared;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        uint heal = 0;
        if (target is not MobEntity || (target.Stats.Mode & MobMode.Mvp) == 0)
        {
            heal = (uint)((120 * skillLevel) + ((target.Stats.MaxHp * skillLevel) / 100));
            _statusOps?.Heal(target, heal, 0, 0);
        }

        // Cure chance: (5*lv + (DEX + casterLv)/4) - rnd(1..10) %.
        var rawChance = skillLevel * 5 + (src.Stats.Dex + src.Level) / 4 - _rng.Next(1, 11);
        if (rawChance > 0 && _rng.Next(100) < rawChance)
        {
            ctx.Sc?.End(target, StatusType.Stone);
            ctx.Sc?.End(target, StatusType.Freeze);
            ctx.Sc?.End(target, StatusType.Stun);
            ctx.Sc?.End(target, StatusType.Poison);
            ctx.Sc?.End(target, StatusType.Silence);
            ctx.Sc?.End(target, StatusType.Blind);
            ctx.Sc?.End(target, StatusType.Hallucination);
        }

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
