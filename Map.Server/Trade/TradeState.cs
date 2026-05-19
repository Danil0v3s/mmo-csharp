namespace Map.Server.Trade;

/// <summary>
/// State of an in-flight trade between two characters. Mirrors the
/// per-side rAthena fields: <c>sd->trade_partner</c>,
/// <c>sd->state.trading</c>, <c>sd->state.deal_locked</c>,
/// <c>sd->deal.item[]</c>, <c>sd->deal.zeny</c>.
/// </summary>
public sealed class TradeState
{
    public required int SelfCharId { get; init; }
    public required int PartnerCharId { get; init; }
    public int PartnerLevel { get; set; }

    /// <summary>Has the other party accepted the trade request yet.</summary>
    public bool Accepted { get; set; }

    /// <summary>
    /// rAthena <c>deal_locked</c>:
    ///   0 — modifying (can add items)
    ///   1 — locked (pressed OK; waiting for partner to lock)
    ///   2 — committing (both pressed final Trade)
    /// </summary>
    public byte LockedStage { get; set; }

    /// <summary>Items offered (up to 10 slots, rAthena MAX_DEAL_ITEMS).</summary>
    public List<TradeItem> Items { get; } = new(10);

    /// <summary>Zeny offered.</summary>
    public long Zeny { get; set; }
}

/// <summary>One offered slot — server-side inventory index + amount.</summary>
public sealed record TradeItem(int InventoryIndex, int Amount);

/// <summary>Outcome of a trade-side operation. Mirrors rAthena clif_traderesponse codes.</summary>
public enum TradeOpResult
{
    Ok = 0,
    TargetTooFar = 1,
    TargetNotExist = 2,
    Failed = 3,
    AlreadyTrading = 4,
    NotEnoughZeny = 5,
    InventoryFull = 6,
    InvalidStage = 7,
    NotInTrade = 8,
    SlotInvalid = 9,
}
