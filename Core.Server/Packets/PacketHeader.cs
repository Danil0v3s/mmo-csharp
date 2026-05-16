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
    ZC_NOTIFY_HP_TO_GROUPM = 0x0106,
    ZC_AID = 0x0283,
    ZC_ACCEPT_ENTER_ZONE = 0x02eb,         // PACKETVER >= 20160330 zone-server variant
    ZC_REFUSE_ENTER_ZONE = 0x0074,         // alias of HC_REFUSE_ENTER for zone-server clarity
    ZC_NOTIFY_STANDENTRY = 0x09ff,         // idle_unit at PACKETVER >= 20150513
    ZC_ITEM_ENTRY = 0x009d,                // floor item already on map; entering view
    ZC_ITEM_FALL_ENTRY = 0x0add,           // floor item just dropped; PACKETVER >= 20180418
    ZC_ITEM_DISAPPEAR = 0x00a1,            // floor item picked up / despawned

    // Client -> Zone (CZ)
    CZ_NOTIFY_ACTORINIT = 0x007d,          // LoadEndAck — client ready to spawn
    CZ_REQUEST_TIME = 0x007e,
    CZ_REQUEST_MOVE = 0x0085,
    CZ_REQUEST_ACTION = 0x0089,
    CZ_REQUEST_CHAT = 0x008c,
    CZ_REQ_QUIT = 0x018a,
    CZ_HEARTBEAT = 0x0360,
    CZ_ITEM_PICKUP = 0x0362,               // CZ_ITEM_PICKUP2 (modern, 4-byte entity id)
    CZ_WANT_TO_CONNECTION = 0x0436,        // modern post-charselect connect
}
