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

    /// <summary>
    /// Emits the full LoadEndAck broadcast cascade, mirroring rAthena
    /// <c>clif_parse_LoadEndAck</c> ([clif.cpp:10723+]) for a fresh
    /// connect_new (<c>sd-&gt;state.connect_new == 1</c>).
    ///
    /// Order matches <c>dhxj.log</c> line 24 packet-by-packet — see
    /// <c>initial-status-broadcast.md</c> for the verified enumeration.
    /// </summary>
    public void BroadcastLoadEndAck(ClientSession session, CharacterDataResponse ch, uint accountId)
    {
        // 0. clif.cpp:10750 — clif_changelook(LOOK_WEAPON, 0). The captured
        // value 1201 is the default Knife from rAthena's start_items;
        // until item grant on create lands we fall back to that.
        session.EnqueuePacket(new ZC_SPRITE_CHANGE2
        {
            AccountId = accountId,
            LookType = 2,  // LOOK_WEAPON
            Value = ch.Weapon == 0 ? 1201u : ch.Weapon,
            Value2 = 0,
        });

        // 1-4. clif.cpp:10760 — clif_inventorylist begins/ends. Empty
        // inventory body bytes for fresh char; the wire structure is
        // ZC_INVENTORY_START + NORMAL_V6 (empty) + EQUIP_V6 (empty) + END.
        session.EnqueuePacket(new ZC_INVENTORY_START { InvType = 0, Name = string.Empty });
        session.EnqueuePacket(new ZC_INVENTORYLIST_NORMAL_V6 { InvType = 0, Body = Array.Empty<byte>() });
        session.EnqueuePacket(new ZC_INVENTORYLIST_EQUIP_V6 { InvType = 0, Body = Array.Empty<byte>() });
        session.EnqueuePacket(new ZC_INVENTORY_END { InvType = 0, Flag = 0 });

        // 5. clif.cpp:10762 — clif_equipswitch_list, empty for fresh char.
        session.EnqueuePacket(new ZC_EQUIPSWITCH_LIST { Entries = Array.Empty<ZC_EQUIPSWITCH_LIST.EquipSwitchEntry>() });

        // 6-7. clif.cpp:10771-10772 — weight re-emit (yes, again).
        Par(session, SpId.SP_WEIGHT, RenewalFormulas.Weight(ch));
        Par(session, SpId.SP_MAXWEIGHT, RenewalFormulas.MaxWeight(ch));

        // 8. clif.cpp:10829 — clif_map_property(MAPPROPERTY_NOTHING).
        // The 4-byte "flag" field is a packed bitmask of map-mode flags
        // (no_party, no_guild, no_zenny_penalty, etc.). Capture for
        // iz_int03 shows 0x00000600 (= bits 9+10 set: no_zenny_penalty
        // + no_class_change or similar). Hardcoded until the full
        // mapflag system lands.
        session.EnqueuePacket(new ZC_MAPPROPERTY_R2 { MapType = 0, Flag = 0x00000600 });

        // 9-10. clif.cpp:10800 — clif_spawn(self) + map_foreachinallarea
        //   double-emits the self STANDENTRY. Real-content emission lands
        //   when the visibility/spawn unification ships; for now we emit
        //   the two-packet shape so the test parses correctly.
        for (var i = 0; i < 2; i++)
        {
            session.EnqueuePacket(new Core.Server.Packets.Out.ZC.ZC_NOTIFY_STANDENTRY
            {
                ObjectType = 0,                     // PC
                AccountId = (int)accountId,
                CharacterOrEntityId = (int)ch.Character.CharacterId,
                Speed = 150,
                Job = (short)ch.ClassId,
                Sex = 1,                            // TODO: thread from session
                X = (short)ch.PositionX,
                Y = (short)ch.PositionY,
                Dir = 0,
                ClientLevel = (short)ch.BaseLevel,
                Name = ch.Character.Name,
            });
        }

        // 11. clif.cpp:10890 — clif_skillinfoblock. Novice's tree:
        // Basic Skill (NV_BASIC) is the only learned skill for a fresh
        // character. Each entry is 37B; we emit 1 entry = 41B total.
        session.EnqueuePacket(new ZC_SKILLINFO_LIST { Body = new byte[37] });

        // 12-13. clif.cpp:10891-10893 — hotkeys, tab 0 + tab 1.
        session.EnqueuePacket(new ZC_SHORTCUT_KEY_LIST { Rotate = 0, Tab = 0 });
        session.EnqueuePacket(new ZC_SHORTCUT_KEY_LIST { Rotate = 0, Tab = 1 });

        // 14-17. clif.cpp:10895-10898 — exp cascade (4× LONGLONGPAR).
        LongLongPar(session, SpId.SP_BASEEXP, (long)ch.BaseExp);
        LongLongPar(session, SpId.SP_NEXTBASEEXP, NextBaseExp(ch));
        LongLongPar(session, SpId.SP_JOBEXP, (long)ch.JobExp);
        LongLongPar(session, SpId.SP_NEXTJOBEXP, NextJobExp(ch));

        // 18. clif.cpp:10899 — skillpoint.
        Par(session, SpId.SP_SKILLPOINT, (int)ch.SkillPoint);

        // 19-48. clif.cpp:10900 — clif_initialstatus (ZC_STATUS) +
        // the full SP_STR..LUK + renewal-stat cascade. Order at
        // clif.cpp:4154-4186.
        EmitInitialStatus(session, ch);

        // 49. clif.cpp:10999 — clif_partyinvitationstate.
        session.EnqueuePacket(new ZC_PARTY_CONFIG { DenyPartyInvites = 0 });

        // 50. clif.cpp:11001 — clif_equipcheckbox (ZC_CONFIG_NOTIFY, 0x02DA).
        session.EnqueuePacket(new ZC_CONFIG_NOTIFY { OpenEquipWindow = 0 });

        // 51-53. clif.cpp:11003-11004 — pet_autofeed (always emit per
        //   rAthena code; CONFIG_PET_AUTOFEED type=2) + CONFIG_CALL +
        //   CONFIG_HOMUNCULUS_AUTOFEED. Three ZC_CONFIG packets total.
        session.EnqueuePacket(new ZC_CONFIG { Type = 2, Value = 0 });   // CONFIG_PET_AUTOFEED
        session.EnqueuePacket(new ZC_CONFIG { Type = 1, Value = 0 });   // CONFIG_CALL
        session.EnqueuePacket(new ZC_CONFIG { Type = 3, Value = 0 });   // CONFIG_HOMUNCULUS_AUTOFEED

        // 54. clif.cpp:11020 — clif_reputation_list, empty for fresh char.
        // Wire shape: header (2) + packetLength (2) + success (1) + entries[]
        // — 65B body matches capture's 69B (4B header + 65B body).
        session.EnqueuePacket(new ZC_REPUTATION_LIST { Body = new byte[65] });
    }

    /// <summary>
    /// rAthena <c>clif_initialstatus</c> (clif.cpp:4111). Emits ZC_STATUS
    /// followed by the SP_STR..LUK + renewal-stat cascade in the exact
    /// order at clif.cpp:4154-4186.
    /// </summary>
    private static void EmitInitialStatus(ClientSession session, CharacterDataResponse ch)
    {
        // ZC_STATUS — populated from the saved stats. Battle-status
        // derived fields use the same formulas as
        // BroadcastStatusCalcFirst for consistency.
        session.EnqueuePacket(new ZC_STATUS
        {
            StatusPoint = (ushort)Math.Min(ch.StatusPoint, ushort.MaxValue),
            Str = (byte)Math.Min(ch.Str, byte.MaxValue),
            StandardStr = (byte)NeedStatusPoint(ch.Str + 1),
            Agi = (byte)Math.Min(ch.Agi, byte.MaxValue),
            StandardAgi = (byte)NeedStatusPoint(ch.Agi + 1),
            Vit = (byte)Math.Min(ch.Vit, byte.MaxValue),
            StandardVit = (byte)NeedStatusPoint(ch.Vit + 1),
            Int = (byte)Math.Min(ch.IntStat, byte.MaxValue),
            StandardInt = (byte)NeedStatusPoint(ch.IntStat + 1),
            Dex = (byte)Math.Min(ch.Dex, byte.MaxValue),
            StandardDex = (byte)NeedStatusPoint(ch.Dex + 1),
            Luk = (byte)Math.Min(ch.Luk, byte.MaxValue),
            StandardLuk = (byte)NeedStatusPoint(ch.Luk + 1),
            AttPower = (short)RenewalFormulas.Batk(ch),                  // leftside
            RefiningPower = (short)RenewalFormulas.WeaponAtk(ch),        // rightside
            MaxMattPower = (short)RenewalFormulas.MatkRight(ch),
            MinMattPower = (short)RenewalFormulas.MatkLeft(ch),
            ItemDefPower = (short)RenewalFormulas.SoftDef(ch),           // leftside (def2)
            PlusDefPower = (short)RenewalFormulas.HardDef(ch),           // rightside (def)
            MdefPower = (short)RenewalFormulas.SoftMdef(ch),
            PlusMdefPower = (short)RenewalFormulas.HardMdef(ch),
            Hit = (short)RenewalFormulas.Hit(ch),
            Flee = (short)RenewalFormulas.Flee(ch),
            Flee2 = (short)RenewalFormulas.Flee2Wire(ch),
            Crit = (short)RenewalFormulas.CriticalWire(ch),
            Aspd = (short)RenewalFormulas.AspdWire(ch),
            PlusAspd = 0,
        });

        // clif.cpp:4154-4159 — SP_STR..SP_LUK as ZC_COUPLESTATUS.
        Couple(session, SpId.SP_STR, (int)ch.Str);
        Couple(session, SpId.SP_AGI, (int)ch.Agi);
        Couple(session, SpId.SP_VIT, (int)ch.Vit);
        Couple(session, SpId.SP_INT, (int)ch.IntStat);
        Couple(session, SpId.SP_DEX, (int)ch.Dex);
        Couple(session, SpId.SP_LUK, (int)ch.Luk);

        // clif.cpp:4161-4162 — SP_ATTACKRANGE + SP_ASPD.
        session.EnqueuePacket(new ZC_ATTACK_RANGE { Range = (short)RenewalFormulas.AttackRange(ch) });
        Par(session, SpId.SP_ASPD, RenewalFormulas.AspdWire(ch));

        // clif.cpp:4165-4170 — renewal SP_POW..SP_CRT as ZC_COUPLESTATUS.
        Couple(session, SpId.SP_POW, (int)ch.Pow);
        Couple(session, SpId.SP_STA, (int)ch.Sta);
        Couple(session, SpId.SP_WIS, (int)ch.Wis);
        Couple(session, SpId.SP_SPL, (int)ch.Spl);
        Couple(session, SpId.SP_CON, (int)ch.Con);
        Couple(session, SpId.SP_CRT, (int)ch.Crt);

        // clif.cpp:4171-4179 — renewal SP_PATK..SP_MAXAP as ZC_PAR_CHANGE.
        Par(session, SpId.SP_PATK, 0);     // = pow/3 + con/5 = 0 for fresh
        Par(session, SpId.SP_SMATK, 0);
        Par(session, SpId.SP_RES, 0);
        Par(session, SpId.SP_MRES, 0);
        Par(session, SpId.SP_HPLUS, 0);
        Par(session, SpId.SP_CRATE, 0);
        Par(session, SpId.SP_TRAITPOINT, (int)ch.TraitPoint);
        Par(session, SpId.SP_AP, (int)ch.Ap);
        Par(session, SpId.SP_MAXAP, (int)ch.MaxAp);

        // clif.cpp:4180-4185 — renewal SP_UPOW..SP_UCRT need-points as
        // ZC_STATUS_CHANGE. Fresh-stat need = 0 (no points spent yet).
        session.EnqueuePacket(new ZC_STATUS_CHANGE { StatusId = SpId.SP_UPOW, Value = (byte)NeedTraitPoint(ch.Pow + 1) });
        session.EnqueuePacket(new ZC_STATUS_CHANGE { StatusId = SpId.SP_USTA, Value = (byte)NeedTraitPoint(ch.Sta + 1) });
        session.EnqueuePacket(new ZC_STATUS_CHANGE { StatusId = SpId.SP_UWIS, Value = (byte)NeedTraitPoint(ch.Wis + 1) });
        session.EnqueuePacket(new ZC_STATUS_CHANGE { StatusId = SpId.SP_USPL, Value = (byte)NeedTraitPoint(ch.Spl + 1) });
        session.EnqueuePacket(new ZC_STATUS_CHANGE { StatusId = SpId.SP_UCON, Value = (byte)NeedTraitPoint(ch.Con + 1) });
        session.EnqueuePacket(new ZC_STATUS_CHANGE { StatusId = SpId.SP_UCRT, Value = (byte)NeedTraitPoint(ch.Crt + 1) });
    }

    /// <summary>
    /// rAthena <c>pc_need_status_point</c>: status points needed to go
    /// from current stat → next-level stat. For pre-3rd-job stats the
    /// formula is <c>(target / 10 + 2)</c>. Returns 2 for Lv1 stats=1.
    /// </summary>
    private static int NeedStatusPoint(uint target) => (int)(target / 10 + 2);

    /// <summary>
    /// rAthena <c>pc_need_trait_point</c> (pc.cpp:8829). Returns 0 for
    /// classes that don't have 4th-job trait access. For a Novice with
    /// <c>pc_maxparameter(SP_POW) == 0</c>, the early-out at line 8839
    /// returns 0. Hardcoded to 0 until the 4th-job class table lands.
    /// </summary>
    private static int NeedTraitPoint(uint target) => 0;

    /// <summary>
    /// Novice Lv 1 → Lv 2 base exp from rAthena's exp_group_db.yml.
    /// Captured shows 548 for the Lv1 row; that's the renewal value
    /// for the "Novice" exp group. Hardcoded until the exp DB lands.
    /// </summary>
    private static long NextBaseExp(CharacterDataResponse ch) => 548;

    /// <summary>Novice Lv 1 → Lv 2 job exp. Captured = 10.</summary>
    private static long NextJobExp(CharacterDataResponse ch) => 10;

    private static void Par(ClientSession session, ushort varId, int value)
        => session.EnqueuePacket(new ZC_PAR_CHANGE { VarId = varId, Value = value });

    private static void Couple(ClientSession session, ushort statusType, int baseValue, int plus = 0)
        => session.EnqueuePacket(new ZC_COUPLESTATUS { StatusType = statusType, BaseStatus = baseValue, PlusStatus = plus });

    private static void LongLongPar(ClientSession session, ushort varId, long value)
        => session.EnqueuePacket(new ZC_LONGLONGPAR_CHANGE { VarId = varId, Value = value });
}
