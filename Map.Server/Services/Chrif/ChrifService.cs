using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Services.Chrif;

/// <summary>Default <see cref="IChrifService"/>. Shims onto IServerConnectionService when callsites port.</summary>
public sealed class ChrifService : IChrifService
{
    private readonly ILogger<ChrifService> _logger;
    public ChrifService(ILogger<ChrifService> logger) => _logger = logger;

    public int Save(PlayerEntity pc, byte flag) => 0;
    public bool AuthReq(int accountId, int charId, uint loginId1, uint loginId2) => false;
    public int AuthOk(PlayerEntity pc) => 0;
    public bool CharSelectReq(PlayerEntity pc, uint sIp, ushort sPort) => false;
    public bool ChangeMapServer(PlayerEntity pc, ushort mapIndex, short x, short y, uint sIp, ushort sPort) => false;
    public bool ChangeSex(PlayerEntity pc, byte sex) => false;
    public bool ChangeEmail(int accountId, string actualEmail, string newEmail) => false;
    public bool SearchCharId(int charId) => false;
    public int CharnameAck(int charId, string name) => 0;
    public bool BuildFameList() => false;
    public int SaveScData(PlayerEntity pc) => 0;
    public int LoadScData(int accountId, int charId) => 0;
    public int SkillCooldownSave(PlayerEntity pc) => 0;
    public int SkillCooldownRequest(PlayerEntity pc) => 0;
    public int BsdataSave(PlayerEntity pc, bool quit) => 0;
    public int BsdataRequest(PlayerEntity pc) => 0;
    public int Divorce(int charId, int partnerCharId) => 0;
    public int CharDisconnect(PlayerEntity pc) => 0;
    public bool ReqAccData(int accountId, byte type) => false;
    public int ReqFameList() => 0;
    public int Connect() => 0;
    public bool CheckConnectCharServer() => true;
    public bool IsConnected() => true;
    public int KeepAlive() => 0;
    public void Init() { }
    public void Final() { }
}
