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

    public void BroadcastSkillCasting(Entity src, Entity? target, short targetX, short targetY,
        ushort skillId, ushort skillLevel, int castTimeMs)
    {
        // T5.3a — rAthena clif_skillcasting (clif.cpp:5930). The wire
        // packet (PACKET_ZC_USESKILL_ACK / 0x0119 on legacy, 0x07fb on
        // PACKETVER 20091201+) carries the casting bar over the caster.
        // We don't have a dedicated wire packet here yet — log for now
        // so call sites have the canonical entry point, and the
        // OutgoingPacket once the per-packet emitter lands.
        _logger.LogDebug(
            "clif_skillcasting src={Src} target={Target} cell=({X},{Y}) skill={Skill}@{Lv} cast={Cast}ms",
            src.Id, target?.Id, targetX, targetY, skillId, skillLevel, castTimeMs);
    }

    public void BroadcastSkillCastCancel(Entity caster)
    {
        // T5.3a — rAthena clif_skillcastcancel (clif.cpp:5973). Wire
        // packet PACKET_ZC_DISPEL (0x01b9) carries the cast-cancel to
        // AOI; clients clear the casting bar. Same logging-only first
        // slice until the wire emitter lands.
        _logger.LogDebug("clif_skillcastcancel caster={Caster}", caster.Id);
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

    public void BroadcastSkillEstimation(PlayerEntity caster, Entity targetMob)
    {
        // rAthena: clif_skill_estimation packs the target's BattleStats
        // (class, level, size, def, race, mdef, element, ele_lv) and the
        // 9-element attribute damage modifier table. The per-element
        // table reads from rAthena's `attr_fix_table[ele_lv][src_ele][tgt_ele]`
        // — we don't yet expose a public helper for that, so emit zeros
        // (client renders no per-element shading) until the lookup wires.
        ushort classId = 0;
        ushort level = 0;
        ushort size = 0;
        int hp = 0;
        ushort def = 0;
        ushort race = 0;
        ushort mdef = 0;
        ushort element = 0;
        byte elementLv = 0;
        if (targetMob is MobEntity mob)
        {
            classId = (ushort)mob.ClassId;
            level = (ushort)mob.Level;
            size = (ushort)mob.Stats.Size;
            hp = mob.Hp;
            def = (ushort)mob.Stats.Def;
            race = (ushort)mob.Stats.Race;
            mdef = (ushort)mob.Stats.Mdef;
            element = (ushort)mob.Stats.DefenseElement;
            elementLv = mob.Stats.ElementLevel;
        }
        var packet = new ZC_MONSTER_INFO
        {
            ClassId = classId,
            Level = level,
            Size = size,
            Hp = hp,
            Def = def,
            Race = race,
            Mdef = mdef,
            Element = element,
            ElementLv = elementLv,
            // Per-element damage modifier table — left at 0 until the
            // attr_fix lookup helper exposes a public read.
        };
        _visibility.SendToSelf(caster, packet);
    }

    public void BroadcastCookingList(PlayerEntity caster, int produceType, IReadOnlyList<ushort> craftableItemIds)
    {
        var packet = new ZC_MAKABLEITEMLIST
        {
            ListType = (short)produceType,
            ItemIds = craftableItemIds,
        };
        _visibility.SendToSelf(caster, packet);
    }
}
