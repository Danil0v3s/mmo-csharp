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

    // Char
    HC_ACK_CHANGE_CHARACTER_SLOT = 0xb70,  // 2928 in decimal
    HC_ACK_CHARINFO_PER_PAGE = 0xb72,      // 2930 in decimal  
    HC_ACCEPT_MAKECHAR = 0xb6f,            // 2927 in decimal
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
    CH_REQ_CHANGE_CHARNAME = 0x28f,
    CH_REQ_CHANGE_CHARACTERNAME = 0x8fc,
    CH_REQ_CAPTCHA = 0x7e5,
    CH_PINCODE_SETNEW = 0x8ba,
    CH_PINCODE_CHECK = 0x8b8,
    CH_PINCODE_CHANGE = 0x8be,
    CH_MOVE_CHAR_SLOT = 0x8d4,
    CH_KEEP_ALIVE = 0x187,
    CH_MAKE_NEW_CHAR = 0x67,
    CH_MAKE_NEW_CHAR_V2 = 0x970,
    CH_MAKE_NEW_CHAR_V3 = 0xa39,
    
    // HC - Char Server to Client
    HC_SEND_MAP_DATA = 0xac5,
    HC_CHAR_DELETE2_ACCEPT_ACK = 0x82a,
    HC_ACK_IS_VALID_CHARNAME = 0x28e,
    HC_REFUSE_ENTER = 0x6c,
    HC_ACK_CHANGE_CHARNAME = 0x290,
    HC_CHAR_DELETE2_ACK = 0x828,
    HC_BLOCK_CHARACTER = 0x20d,
    HC_ACK_CHANGE_CHARACTERNAME = 0x8fd,
    HC_CHAR_DELETE2_CANCEL_ACK = 0x82c,
    HC_CHARLIST_NOTIFY = 0x9a0,
    HC_CHARACTER_LIST = 0x82d,

    // === Zone/Map Server <-> Client ===
    // Zone -> Client (ZC)
    ZC_ACCEPT_ENTER = 0x0073,
    ZC_REFUSE_ENTER = 0x0074,
    ZC_NOTIFY_MOVE = 0x007b,
    ZC_NOTIFY_PLAYERMOVE = 0x0086,
    ZC_STOPMOVE = 0x0088,
    ZC_ENTITY_LIST = 0x0099,
    ZC_NOTIFY_CHAT = 0x008d,
    ZC_NOTIFY_HP_TO_GROUPM = 0x0106,

    // Client -> Zone (CZ)
    CZ_ENTER = 0x0072,
    CZ_REQUEST_MOVE = 0x0085,
    CZ_REQUEST_TIME = 0x007e,
    CZ_REQUEST_ACTION = 0x0089,
    CZ_REQUEST_CHAT = 0x008c,
    CZ_HEARTBEAT = 0x0360,
}