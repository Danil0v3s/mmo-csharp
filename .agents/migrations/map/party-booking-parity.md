# party.cpp booking parity · 2026-05-20

5 booking-specific functions from `src/map/party.cpp` (1 575 lines,
44 total functions). The main party engine (member roster + EXP
share + leader transfer) is covered by existing PartyService /
PartyShareService; booking is split out so the main service stays
focused.

Booking is covered by [IPartyBookingService](/Map.Server/Party/Booking/IPartyBookingService.cs).
Full party.cpp audit doc lands in a separate pass alongside the
party-engine sweep.

## History

### 2026-05-20 — initial booking sub-audit
- `party_booking_register` / `update` / `search` / `delete` / `load`.
