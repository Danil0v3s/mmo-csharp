using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Auction;

/// <summary>
/// Default <see cref="IAuctionClientService"/> — routes every auction ZC packet to the owning
/// player's own session (all auction packets are SELF-target in rAthena).
/// </summary>
public sealed class AuctionClientService : IAuctionClientService
{
    private readonly ISessionManagerAccessor _sessions;
    private readonly ILogger<AuctionClientService> _logger;

    public AuctionClientService(ISessionManagerAccessor sessions, ILogger<AuctionClientService> logger)
    {
        _sessions = sessions;
        _logger = logger;
    }

    public void OpenWindow(PlayerEntity pc)
        => _sessions.GetByEntityId(pc.Id)?.EnqueuePacket(new ZC_AUCTION_OPENWINDOW { Flag = 0 });

    public void SetItemResult(PlayerEntity pc, short clientIndex, bool fail)
        => _sessions.GetByEntityId(pc.Id)?.EnqueuePacket(new ZC_ACK_AUCTION_ADD_ITEM { Index = clientIndex, Fail = fail });

    public void Message(PlayerEntity pc, AuctionResultMessage flag)
        => _sessions.GetByEntityId(pc.Id)?.EnqueuePacket(new ZC_AUCTION_RESULT { Flag = flag });

    public void CloseResult(PlayerEntity pc, short result)
        => _sessions.GetByEntityId(pc.Id)?.EnqueuePacket(new ZC_AUCTION_ACK_MY_SELL_STOP { Result = result });

    public void SendResults(PlayerEntity pc, int pages, IReadOnlyList<AuctionResultEntry> entries)
        => _sessions.GetByEntityId(pc.Id)?.EnqueuePacket(new ZC_AUCTION_ITEM_REQ_SEARCH { Pages = pages, Auctions = entries });
}
