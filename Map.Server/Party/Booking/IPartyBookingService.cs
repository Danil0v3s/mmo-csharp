using Map.Server.Entities;

namespace Map.Server.Party.Booking;

/// <summary>
/// Party-find / party-booking system. Pulls the booking-related
/// rAthena <c>party.cpp</c> entries out so the main party engine
/// stays focused on member roster + EXP share.
///
/// Canonical entry points for <c>party_booking_*</c> (party.cpp).
/// </summary>
public interface IPartyBookingService
{
    /// <summary>rAthena <c>party_booking_register</c>.</summary>
    bool Register(PlayerEntity owner, short minLevel, short maxLevel, IReadOnlyList<short> jobs);

    /// <summary>rAthena <c>party_booking_update</c>.</summary>
    bool Update(PlayerEntity owner, IReadOnlyList<short> jobs);

    /// <summary>rAthena <c>party_booking_search</c>.</summary>
    int Search(PlayerEntity searcher, short level, short mapId, short job);

    /// <summary>rAthena <c>party_booking_delete</c>.</summary>
    bool Delete(PlayerEntity owner);

    /// <summary>rAthena <c>party_booking_load</c>.</summary>
    void Load();
}
