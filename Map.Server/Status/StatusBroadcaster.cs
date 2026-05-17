using Core.Server.IPC;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;

namespace Map.Server.Status;

/// <summary>
/// Emits the post-handoff stat broadcast cascade. Three entry points
/// matching the three triggered moments in rAthena's connect flow:
///
/// 1. <see cref="BroadcastStatusCalcFirst"/> — after CZ_WANT_TO_CONNECTION,
///    once the inventory IPC reply arrives (rAthena <c>intif_parse_StorageReceived</c>
///    case TABLE_INVENTORY → <c>status_calc_pc(SCO_FIRST|SCO_FORCE)</c>
///    diff-emit at <c>status.cpp:6338+</c>). For us this is invoked
///    synchronously from <c>WantToConnectionHandler</c> because the
///    auth response already carries the character's saved stats.
///
/// 2. <see cref="BroadcastLoadEndAckUpdates"/> — partway through
///    <c>clif_parse_LoadEndAck</c> (<c>clif.cpp:10771-10773</c>): weight
///    re-emit, then on connect_new the exp + skillpoint cascade
///    (<c>clif.cpp:10895-10899</c>). (Not implemented yet — slice B.)
///
/// 3. <see cref="BroadcastInitialStatus"/> — invokes <c>clif_initialstatus</c>
///    (<c>clif.cpp:4111</c>) followed by every SP_*-stat update at
///    <c>clif.cpp:4154-4186</c>. (Not implemented yet — slice B.)
///
/// All per-stat values come from <see cref="RenewalFormulas"/>. The
/// emit order is the exact order rAthena's diff loop produces against
/// a zeroed b_status — verified byte-for-byte against
/// <c>dhxj.log</c> line 13 trailing bytes.
/// </summary>
public sealed class StatusBroadcaster
{
    /// <summary>
    /// Mirrors the diff-emit loop in <c>status.cpp:6338-6457</c> for a
    /// fresh <c>status_calc_pc(SCO_FIRST)</c>. Pre-calc battle status
    /// is all-zero, so every non-zero post-calc stat fires a packet.
    /// Renewal's duplicate DEF1/DEF2 + MDEF1/MDEF2 emits at lines
    /// 6369-6390 and 6406-6417 are reproduced exactly.
    /// </summary>
    public void BroadcastStatusCalcFirst(ClientSession session, CharacterDataResponse ch)
    {
        // status_calc_weight tail: SP_MAXWEIGHT (status.cpp:3648).
        Par(session, SpId.SP_MAXWEIGHT, RenewalFormulas.MaxWeight(ch));

        // status.cpp:6341-6352 — basic stats as ZC_COUPLESTATUS.
        Couple(session, SpId.SP_STR, (int)ch.Str);
        Couple(session, SpId.SP_AGI, (int)ch.Agi);
        Couple(session, SpId.SP_VIT, (int)ch.Vit);
        Couple(session, SpId.SP_INT, (int)ch.IntStat);
        Couple(session, SpId.SP_DEX, (int)ch.Dex);
        Couple(session, SpId.SP_LUK, (int)ch.Luk);

        // status.cpp:6354-6360 — hit/flee/aspd.
        Par(session, SpId.SP_HIT, RenewalFormulas.Hit(ch));
        Par(session, SpId.SP_FLEE1, RenewalFormulas.Flee(ch));
        Par(session, SpId.SP_ASPD, RenewalFormulas.AspdWire(ch));

        // status.cpp:6362-6373 — atk1 (batk diff), then def-diff which
        // fires SP_DEF1 (=def2 in renewal display) AND SP_DEF2 (=def).
        Par(session, SpId.SP_ATK1, RenewalFormulas.Batk(ch));
        Par(session, SpId.SP_DEF1, RenewalFormulas.SoftDef(ch));   // leftside / def2
        Par(session, SpId.SP_DEF2, RenewalFormulas.HardDef(ch));   // rightside / def

        // status.cpp:6376-6383 — atk2 diff fires SP_ATK2 (=watk+watk2+eatk).
        Par(session, SpId.SP_ATK2, RenewalFormulas.WeaponAtk(ch));

        // status.cpp:6385-6390 — def2-diff. Renewal emits BOTH again
        // in opposite order (hard def first, soft def second).
        Par(session, SpId.SP_DEF2, RenewalFormulas.HardDef(ch));
        Par(session, SpId.SP_DEF1, RenewalFormulas.SoftDef(ch));

        // status.cpp:6391-6394 — flee2 + critical.
        Par(session, SpId.SP_FLEE2, RenewalFormulas.Flee2Wire(ch));
        Par(session, SpId.SP_CRITICAL, RenewalFormulas.CriticalWire(ch));

        // status.cpp:6396-6404 — matk (renewal collapses min/max into
        // a SP_MATK2 then SP_MATK1 pair).
        Par(session, SpId.SP_MATK2, RenewalFormulas.MatkLeft(ch));
        Par(session, SpId.SP_MATK1, RenewalFormulas.MatkRight(ch));

        // status.cpp:6412-6417 — mdef2-diff fires SP_MDEF2 (=mdef) first,
        // then SP_MDEF1 (=mdef2 in renewal display). No mdef-diff fires
        // because hard mdef stays 0 for unarmored Novice.
        Par(session, SpId.SP_MDEF2, RenewalFormulas.HardMdef(ch));
        Par(session, SpId.SP_MDEF1, RenewalFormulas.SoftMdef(ch));

        // status.cpp:6418-6419 — attack range.
        session.EnqueuePacket(new ZC_ATTACK_RANGE { Range = (short)RenewalFormulas.AttackRange(ch) });

        // status.cpp:6420-6427 — maxhp / maxsp / hp / sp (in that order).
        Par(session, SpId.SP_MAXHP, RenewalFormulas.MaxHp(ch));
        Par(session, SpId.SP_MAXSP, RenewalFormulas.MaxSp(ch));
        Par(session, SpId.SP_HP, (int)ch.Hp);
        Par(session, SpId.SP_SP, (int)ch.Sp);

        // Trailing status_calc_weight SP_WEIGHT update (status.cpp:3646).
        Par(session, SpId.SP_WEIGHT, RenewalFormulas.Weight(ch));

        // Tail: mail / achievement / overweight tail. These fire from
        // separate IPC replies in rAthena; for the capture's empty case
        // we emit defaults so the wire structurally matches.
        session.EnqueuePacket(new ZC_NOTIFY_UNREADMAIL { Result = 0 });
        for (var i = 0; i < 3; i++)
            session.EnqueuePacket(new ZC_ACH_UPDATE());           // 3 default entries
        session.EnqueuePacket(new ZC_ALL_ACH_LIST { Body = new byte[68] }); // 72B total (4B header + 68)
        // ZC_OVERWEIGHT_PERCENT carries the *threshold* (not current
        // weight%) — rAthena `battle_config.natural_heal_weight_rate_renewal`
        // default 70 (clif.cpp:22005). It's the percentage at which the
        // weight UI turns red / natural heal stops.
        session.EnqueuePacket(new ZC_OVERWEIGHT_PERCENT { Percent = 70 });
    }

    private static void Par(ClientSession session, ushort varId, int value)
        => session.EnqueuePacket(new ZC_PAR_CHANGE { VarId = varId, Value = value });

    private static void Couple(ClientSession session, ushort statusType, int baseValue, int plus = 0)
        => session.EnqueuePacket(new ZC_COUPLESTATUS { StatusType = statusType, BaseStatus = baseValue, PlusStatus = plus });
}
