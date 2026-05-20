using Map.Server.Entities;

namespace Map.Server.Services.Chrif;

/// <summary>
/// Map → char IPC façade. Canonical entry points for rAthena
/// <c>chrif.cpp</c> (1 974 lines, 67 public functions).
///
/// The C# port already has `IServerConnectionService` + typed
/// `CharServerIpcService` wrappers covering the gRPC channels.
/// This service exposes the rAthena-named operations
/// (chrif_save, chrif_authreq, chrif_charselectreq, chrif_changemapserver,
/// …) so a port can reach for a single named call.
/// </summary>
public interface IChrifService
{
    /// <summary>rAthena <c>chrif_save</c>.</summary>
    int Save(PlayerEntity pc, byte flag);
    /// <summary>rAthena <c>chrif_authreq</c>.</summary>
    bool AuthReq(int accountId, int charId, uint loginId1, uint loginId2);
    /// <summary>rAthena <c>chrif_authok</c>.</summary>
    int AuthOk(PlayerEntity pc);
    /// <summary>rAthena <c>chrif_charselectreq</c>.</summary>
    bool CharSelectReq(PlayerEntity pc, uint sIp, ushort sPort);
    /// <summary>rAthena <c>chrif_changemapserver</c>.</summary>
    bool ChangeMapServer(PlayerEntity pc, ushort mapIndex, short x, short y, uint sIp, ushort sPort);
    /// <summary>rAthena <c>chrif_changesex</c>.</summary>
    bool ChangeSex(PlayerEntity pc, byte sex);
    /// <summary>rAthena <c>chrif_changeemail</c>.</summary>
    bool ChangeEmail(int accountId, string actualEmail, string newEmail);
    /// <summary>rAthena <c>chrif_searchcharid</c>.</summary>
    bool SearchCharId(int charId);
    /// <summary>rAthena <c>chrif_charname_ack</c>.</summary>
    int CharnameAck(int charId, string name);
    /// <summary>rAthena <c>chrif_buildfamelist</c>.</summary>
    bool BuildFameList();
    /// <summary>rAthena <c>chrif_save_scdata</c>.</summary>
    int SaveScData(PlayerEntity pc);
    /// <summary>rAthena <c>chrif_load_scdata</c>.</summary>
    int LoadScData(int accountId, int charId);
    /// <summary>rAthena <c>chrif_skillcooldown_save</c>.</summary>
    int SkillCooldownSave(PlayerEntity pc);
    /// <summary>rAthena <c>chrif_skillcooldown_request</c>.</summary>
    int SkillCooldownRequest(PlayerEntity pc);
    /// <summary>rAthena <c>chrif_bsdata_save</c>.</summary>
    int BsdataSave(PlayerEntity pc, bool quit);
    /// <summary>rAthena <c>chrif_bsdata_request</c>.</summary>
    int BsdataRequest(PlayerEntity pc);
    /// <summary>rAthena <c>chrif_divorce</c>.</summary>
    int Divorce(int charId, int partnerCharId);
    /// <summary>rAthena <c>chrif_chardisconnect</c>.</summary>
    int CharDisconnect(PlayerEntity pc);
    /// <summary>rAthena <c>chrif_reqaccdata</c>.</summary>
    bool ReqAccData(int accountId, byte type);
    /// <summary>rAthena <c>chrif_reqfamelist</c>.</summary>
    int ReqFameList();
    /// <summary>rAthena <c>chrif_connect</c>.</summary>
    int Connect();
    /// <summary>rAthena <c>chrif_check_connect_char_server</c>.</summary>
    bool CheckConnectCharServer();
    /// <summary>rAthena <c>chrif_isconnected</c>.</summary>
    bool IsConnected();
    /// <summary>rAthena <c>chrif_keepalive</c>.</summary>
    int KeepAlive();
    /// <summary>rAthena <c>do_init_chrif</c>.</summary>
    void Init();
    /// <summary>rAthena <c>do_final_chrif</c>.</summary>
    void Final();
}
