using Microsoft.Extensions.Logging;

namespace Map.Server.Logging;

/// <summary>
/// Default <see cref="IGameLogService"/>. All log methods just write
/// a structured info-level line through <see cref="ILogger"/> for
/// now — the SQL log tables (`picklog`, `zenylog`, `mvplog`,
/// `chatlog`, `cashlog`, `branchlog`, `feedinglog`, `npclog`) land
/// when their EF Core entities ship. The `atcommandlog` table is
/// already covered by <see cref="Gm.AtCommandLogger"/>.
/// </summary>
public sealed class GameLogService : IGameLogService
{
    private readonly ILogger<GameLogService> _logger;
    public GameLogService(ILogger<GameLogService> logger) => _logger = logger;

    public void Atcommand(int accountId, int charId, string charName, string command)
        => _logger.LogInformation("atcommand acc={Acc} cid={Cid} {Name}: {Cmd}", accountId, charId, charName, command);

    public void Branch(int accountId, int charId, string mapName, int mobId)
        => _logger.LogInformation("branch acc={Acc} cid={Cid} {Map} mob={Mob}", accountId, charId, mapName, mobId);

    public void Cash(int accountId, int charId, long amount, string reason)
        => _logger.LogInformation("cash acc={Acc} cid={Cid} amt={Amt} {Reason}", accountId, charId, amount, reason);

    public void Chat(string scope, int srcId, int dstId, string text)
        => _logger.LogInformation("chat {Scope} src={Src} dst={Dst}: {Text}", scope, srcId, dstId, text);

    public void Feeding(int accountId, int charId, byte target, int itemId)
        => _logger.LogInformation("feeding acc={Acc} cid={Cid} t={T} item={Item}", accountId, charId, target, itemId);

    public void MvpDrop(int charId, int mobId, int itemId)
        => _logger.LogInformation("mvpdrop cid={Cid} mob={Mob} item={Item}", charId, mobId, itemId);

    public void Npc(int accountId, int charId, string text)
        => _logger.LogInformation("npc acc={Acc} cid={Cid}: {Text}", accountId, charId, text);

    public void Pick(byte who, int srcId, char operation, int itemId, int amount)
        => _logger.LogInformation("pick who={Who} src={Src} op={Op} item={Item}x{Amt}", who, srcId, operation, itemId, amount);

    public void PickPc(int charId, char operation, int itemId, int amount)
        => Pick((byte)'P', charId, operation, itemId, amount);

    public void PickMob(int mobId, char operation, int itemId, int amount)
        => Pick((byte)'M', mobId, operation, itemId, amount);

    public void Zeny(int srcCharId, int dstCharId, long amount, string reason)
        => _logger.LogInformation("zeny src={Src} dst={Dst} amt={Amt} {Reason}", srcCharId, dstCharId, amount, reason);

    public void SetDefaults() { }
    public bool ConfigRead(string configPath) => true;
}
