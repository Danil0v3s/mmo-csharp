using Map.Server.Entities;

namespace Map.Server.Trade.Wire;

/// <summary>
/// Wire-level trade entry points. Canonical for rAthena
/// <c>trade.cpp</c> (666 lines, 9 functions). The actual trade
/// engine + validation lives in <see cref="Map.Server.Trade.TradeService"/>;
/// this service maps the rAthena-named request → response stages
/// (request / ack / ok / additem / addzeny / commit / cancel) so
/// callers can match the C++ flow.
/// </summary>
public interface ITradeWireService
{
    /// <summary>rAthena <c>trade_traderequest</c>.</summary>
    void TradeRequest(PlayerEntity src, PlayerEntity target);

    /// <summary>rAthena <c>trade_tradeack</c>.</summary>
    void TradeAck(PlayerEntity src, byte type);

    /// <summary>rAthena <c>trade_tradeadditem</c>.</summary>
    void TradeAddItem(PlayerEntity src, short inventoryIndex, short amount);

    /// <summary>rAthena <c>trade_tradeaddzeny</c>.</summary>
    void TradeAddZeny(PlayerEntity src, int amount);

    /// <summary>rAthena <c>trade_tradeok</c>.</summary>
    void TradeOk(PlayerEntity src);

    /// <summary>rAthena <c>trade_tradecancel</c>.</summary>
    void TradeCancel(PlayerEntity src);

    /// <summary>rAthena <c>trade_tradecommit</c>.</summary>
    void TradeCommit(PlayerEntity src);

    /// <summary>rAthena <c>trade_check</c>.</summary>
    bool TradeCheck(PlayerEntity src);

    /// <summary>rAthena <c>impossible_trade_check</c>.</summary>
    bool ImpossibleTradeCheck(PlayerEntity src);
}
