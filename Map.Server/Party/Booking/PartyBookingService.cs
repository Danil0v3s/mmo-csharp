using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Party.Booking;

/// <summary>Default <see cref="IPartyBookingService"/>. Transient booking registry.</summary>
public sealed class PartyBookingService : IPartyBookingService
{
    private readonly Dictionary<EntityId, Listing> _listings = new();
    private readonly ILogger<PartyBookingService> _logger;
    public PartyBookingService(ILogger<PartyBookingService> logger) => _logger = logger;

    public bool Register(PlayerEntity owner, short minLevel, short maxLevel, IReadOnlyList<short> jobs)
    {
        _listings[owner.Id] = new Listing { MinLevel = minLevel, MaxLevel = maxLevel, Jobs = jobs.ToList() };
        return true;
    }

    public bool Update(PlayerEntity owner, IReadOnlyList<short> jobs)
    {
        if (!_listings.TryGetValue(owner.Id, out var l)) return false;
        l.Jobs = jobs.ToList();
        return true;
    }

    public int Search(PlayerEntity searcher, short level, short mapId, short job) => _listings.Count;
    public bool Delete(PlayerEntity owner) => _listings.Remove(owner.Id);
    public void Load() { /* DB load deferred per PARITY-REMAINING.md §P2.2.e — listings are session-scoped today */ }

    private sealed class Listing
    {
        public short MinLevel;
        public short MaxLevel;
        public List<short> Jobs = new();
    }
}
