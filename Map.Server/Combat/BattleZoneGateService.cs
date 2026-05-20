using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Combat;

/// <summary>
/// Default <see cref="IBattleZoneGateService"/>. BG / GvG team
/// membership lives on session state that hasn't fully landed yet
/// (the BG team id + GvG guild-ally edges). For now we use the
/// same same-party / same-guild rule that DamageService.CanDamage
/// uses, which is the conservative parity choice.
///
/// AI exception reads <see cref="MobEntity.Stats"/>.<c>Mode</c>.
/// </summary>
public sealed class BattleZoneGateService : IBattleZoneGateService
{
    public bool CanHitBgTarget(Entity src, Entity target)
    {
        // BG team data: rAthena uses sd->bg_id. Until it joins
        // MapSessionData we fall back to the same-guild rule —
        // conservative: same guild → can't hit.
        if (src is PlayerEntity sp && target is PlayerEntity tp)
        {
            if (sp.GuildId != 0 && sp.GuildId == tp.GuildId) return false;
        }
        return true;
    }

    public bool CanHitGvgTarget(Entity src, Entity target)
    {
        // GvG: same-guild + same-alliance-guild. We don't yet track
        // guild alliance edges — fall back to same-guild only.
        if (src is PlayerEntity sp && target is PlayerEntity tp)
        {
            if (sp.GuildId != 0 && sp.GuildId == tp.GuildId) return false;
        }
        return true;
    }

    public bool HasAiException(MobEntity mob)
    {
        // rAthena flags any mob with MD_DETECTOR or special modes
        // as AI-exempt for certain checks. We treat the "no random
        // walk" mode as the AI exception sentinel — matches the
        // mainline use cases (Treasure Box, etc.).
        return (mob.Stats.Mode & MobMode.NoRandomWalk) != 0;
    }
}
