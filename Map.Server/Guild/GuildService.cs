using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Guild;

/// <summary>Default <see cref="IGuildService"/>. Forwards to char-server IPC when wired.</summary>
public sealed class GuildService : IGuildService
{
    private readonly ILogger<GuildService> _logger;
    public GuildService(ILogger<GuildService> logger) => _logger = logger;

    public int Create(PlayerEntity master, string name) => 0;
    public bool Invite(PlayerEntity inviter, PlayerEntity invitee) => false;
    public bool ReplyInvite(PlayerEntity invitee, int guildId, byte ok) => false;
    public bool Leave(PlayerEntity pc, int guildId, int accountId, int charId, string reason) => false;
    public bool Expulsion(PlayerEntity gm, int guildId, int accountId, int charId, string reason) => false;
    public int SendMessage(PlayerEntity from, string text) => 0;
    public int RecvMessage(int guildId, string sender, string text) => 0;
    public bool ChangePosition(PlayerEntity gm, int idx, int mode, int exp_mode, string name) => false;
    public int ChangeMemberPosition(int guildId, int accountId, int charId, short idx) => 0;
    public int EmblemChanged(int guildId) => 0;
    public bool SkillUp(PlayerEntity pc, ushort skillId) => false;
    public int AllianceAck(int guildId, int allyId, int accountId, int charId, int flag, string mes) => 0;
    public bool Break(PlayerEntity gm, string name) => false;
    public int CastleDataLoad() => 0;
    public int CastleDataLoadAck(int castleId, int index, int value) => 0;
    public int CastleDataSave(int castleId, int index, int value) => 0;
    public int CheckAlliance(int guild1, int guild2, byte flag) => 0;
    public bool CheckMember(int guildId, PlayerEntity pc) => false;
    public int AddMember(int guildId, PlayerEntity pc) => 0;
    public int SendXyTimer(int guildId) => 0;
    public int SendDotRemove(PlayerEntity pc) => 0;
    public int RecvInfo(int guildId) => 0;
    public int RecvNoInfo(int guildId) => 0;
    public void SendLevelUp(PlayerEntity pc) { }
    public void Reload() { }
    public void Init() { }
    public void Final() { }
}
