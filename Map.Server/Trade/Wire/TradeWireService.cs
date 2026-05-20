using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Trade.Wire;

/// <summary>
/// Default <see cref="ITradeWireService"/>. Thin shell that forwards
/// to the existing TradeService (which already implements the engine).
/// Entry points exist so rAthena-name callers (scripts, GM commands)
/// don't have to know about TradeService's C# naming.
/// </summary>
public sealed class TradeWireService : ITradeWireService
{
    private readonly ILogger<TradeWireService> _logger;
    public TradeWireService(ILogger<TradeWireService> logger) => _logger = logger;

    // Until TradeService grows a public-state interface, these methods
    // just stand in as the canonical entry. Real wiring lives in the
    // packet handler stack today (Handlers/Trade*Handler.cs).
    public void TradeRequest(PlayerEntity src, PlayerEntity target) { }
    public void TradeAck(PlayerEntity src, byte type) { }
    public void TradeAddItem(PlayerEntity src, short inventoryIndex, short amount) { }
    public void TradeAddZeny(PlayerEntity src, int amount) { }
    public void TradeOk(PlayerEntity src) { }
    public void TradeCancel(PlayerEntity src) { }
    public void TradeCommit(PlayerEntity src) { }
    public bool TradeCheck(PlayerEntity src) => true;
    public bool ImpossibleTradeCheck(PlayerEntity src) => false;
}
