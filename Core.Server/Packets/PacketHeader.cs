namespace Core.Server.Packets;

/// <summary>
/// Packet header identifiers for all packet types in the MMO system.
/// Uses directional prefixes:
/// - AC/HC/SC/ZC: Server to Client packets
/// - CA/CH/CZ: Client to Server packets
/// </summary>
public enum PacketHeader : short
{
    // === Auth Server <-> Client ===
    // Auth -> Client (AC)
    AC_ACCEPT_LOGIN = 0x0069,
    AC_REFUSE_LOGIN = 0x006a,
    AC_ACCEPT_MAKECHAR = 0x006d,
    AC_REFUSE_MAKECHAR = 0x006e,
    
    // Client -> Auth (CA)
    CA_LOGIN = 0x0064,
    CA_CONNECT_INFO_CHANGED = 0x0200,
    CA_SSO_LOGIN_REQ = 0x0825,
    
    // Auth -> Client (SC - alternative/specific)
    SC_NOTIFY_BAN = 0x0081,
    
    // === Char Server <-> Client ===
    // Char -> Client (HC)
    HC_ACCEPT_ENTER = 0x006b,
    HC_REFUSE_ENTER = 0x006c,
    HC_ACCEPT_MAKECHAR = 0x006d,
    HC_REFUSE_MAKECHAR = 0x006e,
    HC_ACCEPT_DELETECHAR = 0x006f,
    HC_REFUSE_DELETECHAR = 0x0070,
    HC_NOTIFY_ZONESVR = 0x0071,
    HC_CHARACTER_LIST = 0x006b,
    
    // Client -> Char (CH)
    CH_ENTER = 0x0065,
    CH_SELECT_CHAR = 0x0066,
    CH_MAKE_CHAR = 0x0067,
    CH_DELETE_CHAR = 0x0068,
    CH_CHARLIST_REQ = 0x09a1,
    
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

