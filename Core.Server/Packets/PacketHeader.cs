namespace Core.Server.Packets;

/// <summary>
/// Packet header identifiers for all packet types in the MMO system.
/// Uses directional prefixes:
/// - AC/HC/SC/ZC: Server to Client packets
/// - CA/CH/CZ: Client to Server packets
/// </summary>
public enum PacketHeader : short
{
    // Login
    CA_LOGIN = 0x64,
    AC_ACCEPT_LOGIN = 0xac4,
    AC_REFUSE_LOGIN = 0x83e,
    SC_NOTIFY_BAN = 0x81,
    CA_REQ_HASH = 0x1db,
    AC_ACK_HASH = 0x1dc,
    CA_LOGIN2 = 0x1dd,
    CA_LOGIN3 = 0x1fa,
    CA_CONNECT_INFO_CHANGED = 0x200,
    CA_EXE_HASHCHECK = 0x204,
    CA_LOGIN_PCBANG = 0x277,
    CA_LOGIN4 = 0x27c,
    CA_LOGIN_CHANNEL = 0x2b0,
    CA_SSO_LOGIN_REQ = 0x825,
    CT_AUTH = 0xacf,
    TC_RESULT = 0xae3,


    // Char (PACKETVER 20220406)
    HC_ACCEPT_DELETECHAR = 0x6f,
    HC_REFUSE_DELETECHAR = 0x70,
    HC_REFUSE_MAKECHAR = 0xb6e,
    HC_ACK_CHANGE_CHARACTER_SLOT = 0xb70,
    HC_ACK_CHARINFO_PER_PAGE = 0xb72,
    HC_ACCEPT_MAKECHAR = 0xb6f,
    HC_NOTIFY_ACCESSIBLE_MAPNAME = 0x840,  // 2112 in decimal
    CH_SELECT_ACCESSIBLE_MAPNAME = 0x841,   // 2113 in decimal
    
    // CH - Client to Char Server
    CH_CHECK_CAPTCHA = 0x7e7,
    CH_SELECT_CHAR = 0x66,
    CH_REQ_TO_CONNECT = 0x65,
    CH_REQ_PINCODE_WINDOW = 0x8c5,
    CH_REQ_IS_VALID_CHARNAME = 0x28d,
    CH_REQ_CHARLIST = 0x9a1,
    CH_REQ_CHAR_DELETE2_CANCEL = 0x82b,
    CH_REQ_CHAR_DELETE2_ACCEPT = 0x829,
    CH_REQ_CHAR_DELETE2 = 0x827,
    CH_DELETE_CHAR = 0x1fb,
    CH_REQ_CHANGE_CHARNAME = 0x8fc,
    CH_REQ_CHANGE_CHARACTERNAME = 0x8fc,
    CH_REQ_CAPTCHA = 0x7e5,
    CH_PINCODE_SETNEW = 0x8ba,
    CH_PINCODE_CHECK = 0x8b8,
    CH_PINCODE_CHANGE = 0x8be,
    CH_MOVE_CHAR_SLOT = 0x8d4,
    CH_KEEP_ALIVE = 0x187,
    CH_MAKE_NEW_CHAR = 0xa39,
    CH_MAKE_NEW_CHAR_V1 = 0x67,
    CH_MAKE_NEW_CHAR_V2 = 0x970,
    CH_MAKE_NEW_CHAR_V3 = 0xa39,
    
    // HC - Char Server to Client
    HC_SEND_MAP_DATA = 0xac5,
    HC_ACCEPT_ENTER = 0x6b,
    HC_ACCEPT_ENTER2 = 0x82d,
    HC_CHAR_DELETE2_ACCEPT_ACK = 0x82a,
    HC_ACK_IS_VALID_CHARNAME = 0x28e,
    HC_REFUSE_ENTER = 0x6c,
    HC_ACK_CHANGE_CHARNAME = 0x8fd,
    HC_CHAR_DELETE2_ACK = 0x828,
    HC_BLOCK_CHARACTER = 0x20d,
    HC_SECOND_PASSWD_LOGIN = 0x8b9,
    HC_ACK_CHANGE_CHARACTERNAME = 0x8fd,
    HC_CHAR_DELETE2_CANCEL_ACK = 0x82c,
    HC_CHARLIST_NOTIFY = 0x9a0,
    HC_CHARACTER_LIST = 0x82d,

    // === Zone/Map Server <-> Client (PACKETVER 20211103, see PacketVersion.cs) ===
    // Zone -> Client (ZC)
    ZC_NOTIFY_VANISH = 0x0080,
    ZC_NOTIFY_TIME = 0x007f,
    ZC_NOTIFY_MOVE = 0x0086,               // other entity moving (clif_move)
    ZC_NOTIFY_PLAYERMOVE = 0x0087,         // echo to mover (clif_walkok)
    ZC_STOPMOVE = 0x0088,
    ZC_NPCACK_MAPMOVE = 0x0091,
    ZC_ENTITY_LIST = 0x0099,
    ZC_NOTIFY_CHAT = 0x008d,
    ZC_NOTIFY_PLAYERCHAT = 0x008e,         // self-only system message (clif_displaymessage)
    ZC_NOTIFY_HP_TO_GROUPM = 0x0106,
    ZC_AID = 0x0283,
    ZC_ACCEPT_ENTER_ZONE = 0x02eb,         // PACKETVER >= 20160330 zone-server variant
    ZC_REFUSE_ENTER_ZONE = 0x0074,         // alias of HC_REFUSE_ENTER for zone-server clarity
    ZC_NOTIFY_STANDENTRY = 0x09ff,         // idle_unit at PACKETVER >= 20150513
    ZC_ITEM_ENTRY = 0x009d,                // floor item already on map; entering view
    ZC_ITEM_FALL_ENTRY = 0x0add,           // floor item just dropped; PACKETVER >= 20180418
    ZC_ITEM_DISAPPEAR = 0x00a1,            // floor item picked up / despawned
    ZC_NOTIFY_ACT3 = 0x08c8,               // combat action / damage (renewal 32-bit damage)

    // Client -> Zone (CZ)
    CZ_NOTIFY_ACTORINIT = 0x007d,          // LoadEndAck — client ready to spawn
    // CZ_REQUEST_MOVE / ACTION / USE_SKILL all underwent the "20180307 shuffle"
    // in the rAthena packet ladder — the IDs below match what our target DHXJ
    // client (PACKETVER 20220401) actually sends. Old pre-shuffle ids: MOVE
    // 0x0085, ACTION 0x0089, CHAT 0x008c. Confirmed via the live
    // dhxj_trace.log packet-type histogram.
    CZ_REQUEST_MOVE = 0x035f,              // shuffle (was 0x0085)
    CZ_REQUEST_ACTION = 0x0437,            // shuffle (was 0x0089)
    CZ_USE_SKILL_TOID = 0x0438,            // CZ_USE_SKILL2 — <lv>.W <id>.W <target>.L (10B). rAthena shuffle of 0x0113.
    CZ_USE_ITEM = 0x00a7,                  // <index>.W <account_id>.L (8B). Used by potions / scrolls / consumables.
    CZ_STATUS_CHANGE = 0x00bb,             // <status_id>.W <amount>.B (5B). pc_statusup spend status points on stat.
    CZ_UPGRADE_SKILLLEVEL = 0x0112,        // <skill_id>.W (4B). pc_skillup — spend skill point on a skill.
    // --- Player trade (trade.cpp) ---
    CZ_REQ_EXCHANGE_ITEM = 0x00e4,         // <account_id>.L (6B). trade_traderequest.
    CZ_ACK_EXCHANGE_ITEM = 0x00e6,         // <result>.B (3B). 3=accept, 4=cancel.
    CZ_ADD_EXCHANGE_ITEM = 0x00e8,         // <index>.W <amount>.L (8B). Index=0 means add zeny.
    CZ_CONCLUDE_EXCHANGE_ITEM = 0x00eb,    // (2B header). Press OK (lock).
    CZ_CANCEL_EXCHANGE_ITEM = 0x00ed,      // (2B). Press cancel.
    CZ_EXEC_EXCHANGE_ITEM = 0x00ef,        // (2B). Press Trade (commit).
    ZC_REQ_EXCHANGE_ITEM = 0x00e5,         // <name>.24B (26B). Show trade request popup on target.
    ZC_ACK_EXCHANGE_ITEM = 0x00e7,         // <result>.B (3B). Trade request ack.
    ZC_ADD_EXCHANGE_ITEM = 0x00e9,         // <amount>.L <nameid>.W <id>.B <atb>.B <rfn>.B <c1>.W <c2>.W <c3>.W <c4>.W (19B).
    ZC_ACK_ADD_EXCHANGE_ITEM = 0x00ea,     // <index>.W <result>.B (5B).
    ZC_CONCLUDE_EXCHANGE_ITEM = 0x00ec,    // <who>.B (3B). 0=self pressed OK, 1=partner.
    ZC_CANCEL_EXCHANGE_ITEM = 0x00ee,      // (2B). Trade cancelled.
    ZC_EXEC_EXCHANGE_ITEM = 0x00f0,        // <result>.B (3B). 0=success, 1=failure.
    // --- NPC shop (npc.cpp / clif.cpp 12230..12335) ---
    CZ_ACK_SELECT_DEALTYPE = 0x00c5,       // <npc_id>.L <type>.B (7B). Buy/sell choice on NPC dialog.
    ZC_PC_PURCHASE_ITEMLIST = 0x00c6,      // var-len. Send the shop catalog to the client.
    ZC_PC_SELL_ITEMLIST = 0x00c7,          // var-len. Send the player's sellable inventory list.
    CZ_PC_PURCHASE_ITEMLIST = 0x00c8,      // var-len. Player's buy basket.
    CZ_PC_SELL_ITEMLIST = 0x00c9,          // var-len. Player's sell basket.
    ZC_PC_PURCHASE_RESULT = 0x00ca,        // <result>.B (3B). Buy outcome code.
    ZC_PC_SELL_RESULT = 0x00cb,            // <result>.B (3B). Sell outcome (0=ok, 1=fail).
    // --- Account storage (storage.cpp / clif.cpp 13595..13714) ---
    // PACKETVER 20220401 uses the variant-2 IDs (0x0364 / 0x0365) since
    // legacy 0x00f3 / 0x00f5 collided with CZ_REQUEST_CHAT / CZ_REQ_QUIT
    // shuffles. rAthena clif_shuffle.hpp tracks the same shift.
    CZ_MOVE_ITEM_FROM_BODY_TO_STORE = 0x0364, // <index>.W <amount>.L (8B). Put into storage.
    ZC_ADD_ITEM_TO_STORE = 0x00f4,         // <index>.W <amount>.L <nameid>.W <id>.B <atb>.B <rfn>.B <c1>.W <c2>.W <c3>.W <c4>.W (21B).
    CZ_MOVE_ITEM_FROM_STORE_TO_BODY = 0x0365, // <index>.W <amount>.L (8B). Take from storage.
    ZC_DELETE_ITEM_FROM_STORE = 0x00f6,    // <index>.W <amount>.L (8B). Server confirms removal.
    CZ_CLOSE_STORE = 0x00f7,               // (2B). Player closed window.
    ZC_CLOSE_STORE = 0x00f8,               // (2B). Server ack.
    ZC_NOTIFY_STOREITEM_COUNTINFO = 0x00f2,// <current>.W <max>.W (6B). Storage capacity HUD.
    // --- Equip / unequip (clif.cpp:12080..12167) ---
    // PACKETVER 20220401 uses the V5/PACKETVER>=20120925 layouts:
    // wear request takes a 32-bit position bitmask (legacy 0xa9 used 16-bit),
    // and the ack carries the worn-slot bitmask + view-id + result code.
    CZ_REQ_WEAR_EQUIP = 0x0998,            // <index>.W <position>.L (8B). pc_equipitem entry.
    CZ_REQ_TAKEOFF_EQUIP = 0x00ab,         // <index>.W (4B). pc_unequipitem entry.
    ZC_REQ_WEAR_EQUIP_ACK = 0x0999,        // <index>.W <wearLocation>.L <viewid>.W <result>.B (11B).
    ZC_REQ_TAKEOFF_EQUIP_ACK = 0x099a,     // <index>.W <wearLocation>.L <flag>.B (9B).
    // --- Drop-from-inventory (clif.cpp:12016 clif_parse_DropItem) ---
    // Legacy 0x00a2; the 0x0363 variant (CZ_ITEM_THROW2) collides with
    // CZ_REQUEST_TIME on PACKETVER 20220401, so we use the legacy id.
    CZ_ITEM_THROW = 0x00a2,                // <index>.W <amount>.W (6B). pc_dropitem entry.
    // --- Whisper / party / guild chat (clif.cpp:11835 / 13948 / 14565) ---
    // PACKETVER 20220401 keeps the modern ZC_WHISPER variant (0x09de)
    // which carries the sender's AID + isAdmin flag.
    CZ_WHISPER = 0x0096,                   // <len>.W <nick>.24B <message>.?B. clif_parse_WisMessage.
    ZC_WHISPER = 0x09de,                   // <len>.W <senderGID>.L <sender>.24B <isAdmin>.B <message>.?B.
    CZ_REQUEST_CHAT_PARTY = 0x0108,        // <len>.W <text>.?B. clif_parse_PartyMessage.
    ZC_NOTIFY_CHAT_PARTY = 0x0109,         // <len>.W <AID>.L <chatMsg>.?B.
    CZ_GUILD_CHAT = 0x017e,                // <len>.W <text>.?B. clif_parse_GuildMessage.
    ZC_GUILD_CHAT = 0x017f,                // <len>.W <message>.?B.
    CZ_USE_SKILL_TOGROUND = 0x0366,        // CZ_USE_SKILL_TOGROUND2 — <lv>.W <id>.W <x>.W <y>.W (10B). Shuffle of 0x0116.
    CZ_REQUEST_CHAT = 0x00f3,              // shuffle (was 0x008c)
    CZ_REQ_QUIT = 0x018a,                  // clif_parse_QuitGame, 4B (reserved). ALT+E.
    ZC_DISCONNECT_ACK = 0x018b,            // clif_disconnect_ack, 4B (result). 0=ok-to-quit, 1=refused.
    CZ_RESTART = 0x00b2,                   // clif_parse_Restart, 3B (type). 0=respawn, 1=char-select.
    ZC_RESTART_ACK = 0x00b3,               // clif_charselectok, 3B (type). 1=disconnect-and-charselect.
    CZ_HEARTBEAT = 0x0360,
    CZ_ITEM_PICKUP = 0x0362,               // CZ_ITEM_PICKUP2 (modern, 4-byte entity id)
    CZ_REQUEST_TIME = 0x0363,              // PACKETVER ≥ 20220401 ticksend / heartbeat (rAthena moved it from 0x007e to 0x0363). Doubles as the keep-alive on our target client; the handler resets HeartbeatTimeout on receipt.
    CZ_WANT_TO_CONNECTION = 0x0436,        // modern post-charselect connect
    CZ_CONTACTNPC = 0x0090,                // clif_parse_NpcClicked, 7B (header + npcId(4) + type(1)). Not part of the 20180307 shuffle.
    CZ_CHOOSE_MENU = 0x00b8,               // clif_parse_ChooseMenu, 7B (npcId + selection). 1-based; 255=Escape.
    CZ_REQ_NEXT_SCRIPT = 0x00b9,           // clif_parse_NextScript, 6B (npcId).
    CZ_CLOSE_DIALOG = 0x0146,              // clif_parse_CloseScript, 6B (npcId). NOTE: distinct from outgoing ZC_CLOSE_DIALOG=0x00b6.
    CZ_REQNAME = 0x0368,                   // clif_parse_SolveCharName, 6B (entityId). Sent on hover / click; expects an ACK_REQNAMEALL[_NPC] in reply. PACKETVER 20180307 shuffle ID.

    ZC_ACK_REQNAMEALL = 0x0a30,            // clif_name for PC, 106B (gid + char[24] + party[24] + guild[24] + position[24] + titleId(4)). PACKETVER ≥ 20150225.
    ZC_ACK_REQNAMEALL_NPC = 0x0adf,        // clif_name for NPC, 58B (gid + groupId(4) + name[24] + title[24]). PACKETVER ≥ 20180207.

    // Dialog packets. dhxj is pre-20220504 — it knows 0x00b4 / 0x00b5 in
    // its packet length table (parses them with correct length) but its UI
    // handler doesn't render the dialog on receipt. The post-20220504 IDs
    // 0x0972 / 0x0973 are completely unknown to dhxj (desync). Pending a
    // network trace of a working dhxj↔real-server NPC conversation to see
    // what the real protocol is.
    ZC_SAY_DIALOG = 0x00b4,                // clif_scriptmes, variable (npcId + asciz message).
    ZC_WAIT_DIALOG = 0x00b5,               // clif_scriptnext, 6B (npcId). Shows the "Next" button.
    ZC_MENU_LIST = 0x00b7,                 // clif_scriptmenu, variable (npcId + colon-joined options).
    ZC_EXTEND_BODYITEM_SIZE = 0x0b18,      // inventory-expansion info; rAthena clif_inventory_expansion_info
    ZC_FRIENDS_LIST = 0x0201,              // rAthena clif_friendslist_send, variable length

    // === Status broadcast cascade (post-handoff via pc_authok / status_calc_pc) ===
    // See .agents/migrations/map/initial-status-broadcast.md for the full enumeration.
    ZC_PAR_CHANGE         = 0x00b0,        // clif_par_change — i16 varId + i32 value
    ZC_LONGPAR_CHANGE     = 0x00b1,        // clif_longpar_change — same shape as 0x00B0 (legacy long path)
    ZC_STATUS             = 0x00bd,        // clif_initialstatus full snapshot, 44B
    ZC_STATUS_CHANGE      = 0x00be,        // clif_zc_status_change — i16 statusId + u8 value
    ZC_SKILLINFO_LIST     = 0x010f,        // clif_skillinfoblock, variable
    ZC_ATTACK_RANGE       = 0x013a,        // clif_attackrange, 4B
    ZC_COUPLESTATUS       = 0x0141,        // SP_STR..LUK / renewal stats — u32 statusType + base + plus
    ZC_SPRITE_CHANGE2     = 0x01d7,        // clif_changelook for PACKETVER ≥ 20181121, 15B
    ZC_PARTY_CONFIG       = 0x02c9,        // clif_partyinvitationstate, 3B
    ZC_CONFIG             = 0x02d9,        // clif_configuration / clif_equipcheckbox, 10B
    ZC_CONFIG_NOTIFY      = 0x02da,        // clif_equipcheckbox (PACKETVER ≥ 20070918), 3B
    ZC_ITEM_THROW_ACK     = 0x00af,        // legacy/unused in PACKETVER 20211103, 6B (header + index + count)
    ZC_CLOSE_DIALOG       = 0x00b6,        // clif_close_dialog, 6B (header + uint32 npcId)
    ZC_BROADCAST2         = 0x01c3,        // intif_broadcast2 (coloured GM-style messages), variable
    ZC_QUEST_NOTIFY_EFFECT = 0x0446,       // clif_quest_show_event (NPC quest icon), 14B
    ZC_MAPPROPERTY_R2     = 0x099b,        // clif_map_property, 8B
    ZC_NAVIGATION_TO      = 0x08e2,        // clif_navigateTo (PACKETVER ≥ 20111010), 27B
    ZC_MSG_STATE_CHANGE3  = 0x0983,        // clif_status_change3 (status icon w/ tick), 29B
    ZC_STATE_CHANGE3      = 0x0229,        // clif_changeoption — option (effect-state) bitmask 32-bit, 15B
    ZC_SPIRITS            = 0x01d0,        // clif_spiritball — Monk sphere / Sura / servant / abyss count, 8B
    ZC_SOULENERGY         = 0x0b73,        // clif_soulball — Soul Reaper soul count, 8B
    ZC_NOTIFY_UNREADMAIL  = 0x09e7,        // mail load reply, 3B
    ZC_ALL_ACH_LIST       = 0x0a23,        // clif_achievement_list_all, variable
    ZC_ACH_UPDATE         = 0x0a24,        // clif_achievement_update, 66B
    ZC_EQUIPSWITCH_LIST   = 0x0a9b,        // clif_equipswitch_list, variable
    ZC_MAIL_NEW_NOTIFY    = 0x0ac2,        // mail / rodex update (PACKETVER ≥ 20170419), 5B
    ZC_LONGLONGPAR_CHANGE = 0x0acb,        // clif_longlongpar_change for SP_BASEEXP/JOBEXP at PACKETVER ≥ 20170830, 12B
    ZC_OVERWEIGHT_PERCENT = 0x0ade,        // pc_updateweightstatus, 6B
    ZC_INVENTORY_START    = 0x0b08,        // clif_inventorystart, variable
    ZC_INVENTORYLIST_NORMAL_V6 = 0x0b09,   // normal-items chunk, variable
    ZC_INVENTORY_END      = 0x0b0b,        // clif_inventoryend, 4B
    ZC_SHORTCUT_KEY_LIST  = 0x0b20,        // clif_hotkeys_send for PACKETVER ≥ 20190522, fixed 271B
    ZC_PAR_4JOB_CHANGE    = 0x0b25,        // 4-job stat coupled change, 14B
    ZC_REPUTATION_LIST    = 0x0b8d,        // clif_reputation_list (rAthena name: ZC_REPUTE_INFO), variable
    ZC_INVENTORYLIST_EQUIP_V6 = 0x0b39,    // equip-items chunk, variable
    ZC_ITEM_PICKUP_ACK    = 0x0b41,        // clif_additem (PACKETVER_RE_NUM >= 20200723), 70B

    // Internal — health-check packets used by the test harness to wait for a
    // server to finish booting. NOT part of the rAthena packet set; picked
    // from the 0x75xx range which rAthena never uses for client packets.
    CZ_INTERNAL_PING = 0x7530,
    ZC_INTERNAL_PONG = 0x7531,
}
