using Map.Server.Entities;

namespace Map.Server.Trade;

/// <summary>
/// Player-to-player trade owner. Port of rAthena trade.cpp:
/// <c>trade_traderequest</c> / <c>trade_tradeack</c> /
/// <c>trade_tradeadditem</c> / <c>trade_tradeaddzeny</c> /
/// <c>trade_tradeok</c> / <c>trade_tradecancel</c> /
/// <c>trade_tradecommit</c>.
///
/// State lives on each <see cref="MapSessionData.Trade"/> while a deal
/// is in progress. Commit is atomic — both sides must have their
/// <c>LockedStage</c> == 2 before any item moves.
/// </summary>
public interface ITradeService
{
    /// <summary>rAthena <c>TRADE_DISTANCE</c> default = 5 cells.</summary>
    const int TradeDistance = 5;
    /// <summary>rAthena <c>MAX_DEAL_ITEMS</c> = 10 slots per side.</summary>
    const int MaxDealItems = 10;

    /// <summary>Initiator asks <paramref name="target"/> to trade.</summary>
    TradeOpResult Request(PlayerEntity initiator, PlayerEntity target);

    /// <summary>Target accepts or declines the request.</summary>
    TradeOpResult Acknowledge(PlayerEntity target, bool accept);

    /// <summary>Initiator's side adds an inventory slot to the deal.</summary>
    TradeOpResult AddItem(PlayerEntity side, int inventoryIndex, int amount);

    /// <summary>Initiator's side adds zeny to the deal.</summary>
    TradeOpResult AddZeny(PlayerEntity side, long zeny);

    /// <summary>Press OK — locks this side's offer (rAthena deal_locked = 1).</summary>
    TradeOpResult Ok(PlayerEntity side);

    /// <summary>
    /// Press Trade — commits if the partner has already pressed Trade.
    /// Returns <see cref="TradeOpResult.Ok"/> if the trade completed,
    /// <see cref="TradeOpResult.InvalidStage"/> if still waiting for
    /// the other side.
    /// </summary>
    TradeOpResult Commit(PlayerEntity side);

    /// <summary>Cancel — clears both sides' state.</summary>
    TradeOpResult Cancel(PlayerEntity side);
}
