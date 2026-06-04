using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;

namespace Map.Server.Auction;

/// <summary>
/// Emits the auction-house ZC packets to a player's session (the client-emit hub for the auction
/// handlers). rAthena <c>clif_Auction_*</c>. All auction packets are SELF-target.
/// </summary>
public interface IAuctionClientService
{
    /// <summary>rAthena <c>clif_Auction_openwindow</c> (0x025f).</summary>
    void OpenWindow(PlayerEntity pc);

    /// <summary>rAthena <c>clif_Auction_setitem</c> (0x0256) — staged-item ack. <paramref name="clientIndex"/>
    /// is the client inventory index (server + 2) on success.</summary>
    void SetItemResult(PlayerEntity pc, short clientIndex, bool fail);

    /// <summary>rAthena <c>clif_Auction_message</c> (0x0250) — a status-code message.</summary>
    void Message(PlayerEntity pc, AuctionResultMessage flag);

    /// <summary>rAthena <c>clif_Auction_close</c> — the close/stop result (0 ended, 1 cannot end, 2 bad id).</summary>
    void CloseResult(PlayerEntity pc, short result);

    /// <summary>rAthena <c>clif_Auction_results</c> (0x0252) — the browse/search results page.</summary>
    void SendResults(PlayerEntity pc, int pages, IReadOnlyList<AuctionResultEntry> entries);
}
