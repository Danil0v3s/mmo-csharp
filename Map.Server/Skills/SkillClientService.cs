using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Visibility;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// Default <see cref="ISkillClientService"/> — routes the three
/// skill-result packets through <see cref="IVisibilityService"/>.
///
/// <para>Mirrors how rAthena's <c>clif_skill_*</c> family resolves: pick
/// the packet, fill the wire fields off the caster / target / damage
/// state, then call <c>clif_send(&amp;p, sizeof(p), bl, scope)</c> with
/// the right scope (AREA for nodamage/damage, SELF for fail).</para>
/// </summary>
public sealed class SkillClientService : ISkillClientService
{
    private readonly IVisibilityService _visibility;
    private readonly ILogger<SkillClientService> _logger;

    public SkillClientService(IVisibilityService visibility, ILogger<SkillClientService> logger)
    {
        _visibility = visibility;
        _logger = logger;
    }

    public void BroadcastSkillNoDamage(Entity? src, Entity target, ushort skillId, int healOrLevel, bool success = true)
    {
        var packet = new ZC_USE_SKILL
        {
            SkillId = skillId,
            Level = healOrLevel,
            TargetAid = target.Id.Value,
            SrcAid = src?.Id.Value ?? 0,
            Result = (byte)(success ? 1 : 0),
        };
        // rAthena: clif_send(&p, sizeof(p), &dst, AREA) — broadcast lives
        // on the target's AOI (where the visual lands). When the target
        // is the caster itself (self-buff), the source is the same AOI.
        _visibility.SendToArea(target, packet);
    }

    public void BroadcastSkillDamage(Entity src, Entity target, ushort skillId, ushort skillLevel,
        long damage, int hitCount = 1, DamageActionType action = DamageActionType.SkillDamage)
    {
        // rAthena clamps damage to int32 on this packet — anything beyond
        // 2.1B is graphical only and clipping matches stock behaviour.
        int clampedDamage = damage switch
        {
            > int.MaxValue => int.MaxValue,
            < int.MinValue => int.MinValue,
            _ => (int)damage,
        };

        var packet = new ZC_NOTIFY_SKILL
        {
            SkillId = skillId,
            SrcAid = src.Id.Value,
            TargetId = target.Id.Value,
            StartTime = (uint)Environment.TickCount,
            AttackMotion = src.Stats.Amotion,
            AttackedMotion = target.Stats.Dmotion,
            Damage = clampedDamage,
            Level = (short)skillLevel,
            HitCount = (short)Math.Max(1, hitCount),
            ActionType = action,
        };
        // rAthena: clif_send(&p, sizeof(p), src, AREA) — broadcast lives
        // on the source's AOI (where the cast originated).
        _visibility.SendToArea(src, packet);
    }

    public void BroadcastSkillFail(PlayerEntity caster, ushort skillId, SkillFailCause cause,
        int btype = 0, uint itemId = 0)
    {
        // rAthena guards on battle_config.display_skill_fail&1 — when set,
        // suppress every fail message. We honor the same flag once the
        // battle-config service exposes it; for now always emit so the
        // caster gets feedback during development.
        var packet = new ZC_ACK_TOUSESKILL
        {
            SkillId = skillId,
            Btype = btype,
            ItemId = itemId,
            Flag = 0, // always "failed" on this packet
            Cause = (byte)cause,
        };
        // rAthena: clif_send(&p, sizeof(p), &sd.bl, SELF) — only the
        // caster sees their own fail messages.
        _visibility.SendToSelf(caster, packet);
    }
}
