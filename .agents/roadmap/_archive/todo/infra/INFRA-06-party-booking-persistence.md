# INFRA-06 — Party Booking persistence + filtered search

> **Epic:** Infra parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none

## Problem

The **Party Booking** board (the "looking for party" recruitment list) is session-only and
its search is broken. `Load()` does nothing, so all listings vanish on map-server restart,
and `Search(...)` returns `_listings.Count` — the *total* number of listings, completely
ignoring the level / map / job filter the player typed. A player searching for "level 80
party on prontera" gets a meaningless count and no filtered rows.

## Current state (C#)

- `Map.Server/Party/Booking/PartyBookingService.cs`:
  - `_listings` is a plain `Dictionary<EntityId, Listing>` (`:9`) — transient.
  - `Register(owner, minLevel, maxLevel, jobs)` (`:13-17`) — overwrites the owner's
    listing in memory; no persistence.
  - `Update(owner, jobs)` (`:19-24`) — mutates jobs in memory only.
  - `Search(searcher, level, mapId, job) => _listings.Count` (`:26`) — **ignores all three
    filter args** and returns the raw count.
  - `Delete(owner) => _listings.Remove(owner.Id)` (`:27`) — memory only.
  - `Load() { /* DB load deferred ... */ }` (`:28`) — empty.
  - `Listing` (`:30-35`) holds `MinLevel`, `MaxLevel`, `List<short> Jobs` — note it does
    **not** store the owner's current map or the owner's name/char id, both of which the
    search/result packets need.

## rAthena reference (source of truth)

Canonical source is `clif.cpp` (the `clif_parse_PartyBookingRegisterReq` family) +
`party.cpp` booking helpers. There are two booking protocols in rAthena (the older
`PartyBookingRegisterReq`/`SearchReq`/`DeleteReq` and the newer `PartyRecruit*`); match
whichever the C# handler stack already speaks.

- **Register**: stores `(char_id, account_id, char_name, level, map_id, job[] up to
  MAX_PARTY_BOOKING_JOBS, expiry)` keyed by char/account. Replaces an existing listing
  for the same owner.
- **Search**: filters the active listings by `level` (the listing's level band must
  include the requested level, or the listing level ≥ requested — match the source),
  `map_id` (0 = any map), and `job` (0/-1 = any; otherwise the listing must offer that
  job). Returns a *page* of matching listings (with a "more results" flag), not a count.
- **Delete**: removes the owner's listing; broadcasts the removal so other clients drop
  the row.
- **Expiry**: rAthena expires listings after a TTL (`battle_config` booking expiry) and
  prunes them on a timer.

Booking data in rAthena is **not** SQL-persisted across restarts by default (it lives in
`party_booking_db` in-memory and is rebuilt as players re-register). So "persistence" here
has two valid interpretations — see Scope / gotchas. The minimum parity fix is the
**filtered search**; cross-restart persistence is the stretch (and lowest-impact) half.

## Scope — every sub-system that must be touched

- [ ] **Fix `Search` (the must-do).** Replace `=> _listings.Count` with a real filter:
  - [ ] `level` — keep listings whose `[MinLevel, MaxLevel]` band includes `level`
        (confirm the exact rAthena comparison; some versions match listing level ≥ req).
  - [ ] `mapId` — `0` means any; otherwise the listing's map must equal `mapId`.
  - [ ] `job` — `0`/`-1` means any; otherwise the listing's `Jobs` must contain `job`.
  - [ ] Return the matching listings (page them per the result packet's capacity) and a
        "has more" flag — not a bare count. Update the handler to emit the result rows.
- [ ] **Extend `Listing`** to carry `OwnerCharId`, `OwnerAccountId`, `OwnerName`, and
      `MapId` (the search result + register packet need them; today they're absent).
      Populate `MapId` from the owner's current map at Register time.
- [ ] **Expiry/prune** (parity): add a TTL + a periodic prune (hook into the existing
      map-server tick/observer, like other timed services) so stale listings drop. Match
      the `battle_config` booking-expiry default.
- [ ] **Persistence (stretch / lowest-impact half).** Choose ONE and document the choice:
  - **Option A — match rAthena exactly:** leave listings in-memory (no SQL), and make
    `Load()` a documented no-op that rebuilds from re-registration. If chosen, this ticket
    collapses to the search fix + expiry, and `Load()` gets a real explanatory comment
    (not "deferred").
  - **Option B — durable booking (divergence, only if product wants it):**
    `PartyBookingEntity` (`char_id`, `account_id`, `char_name`, `min_level`, `max_level`,
    `map_id`, `jobs` as CSV or a child table, `expire_time`) +
    `IPartyBookingRepository` + migration (`dotnet ef migrations add DB-PartyBooking`
    from `Core.Database`). `Register`/`Update`/`Delete` write through; `Load()` hydrates
    `_listings` at boot, dropping expired rows. Note booking is party state → per CLAUDE.md
    "no in-memory shortcuts for persisted state" only applies to *persisted* state; since
    rAthena keeps booking in RAM, Option A is the parity-true default — pick B only on an
    explicit product request and record the divergence here.
- [ ] **Client packets**: ensure the search-result packet (`ZC_PARTY_BOOKING_SEARCH_ACK`
      / recruit equivalent), register-ack, and delete-notify are emitted with the listing
      fields. Add definitions under `Core.Server/Packets/Out/ZC/` if missing.

## Done criteria

- A search for level 80 / prontera / Knight returns only listings whose band includes 80,
  whose map is prontera (or any when mapId=0), and that offer Knight (or any when job=0) —
  **not** the total listing count.
- The result is paged with a correct "more" flag.
- Expired listings are pruned and excluded from search.
- (Option A) `Load()` carries a real comment explaining the in-memory parity model, no
  "deferred" wording. (Option B) Listings survive a map-server restart.
- No `=> _listings.Count` and no empty `Load()` stub remain.

## Test plan

- `Map.Server.Tests/Party/PartyBookingServiceTests`:
  - Register 3 listings with different level bands / maps / jobs; assert `Search` returns
    exactly the matching subset for several filter combinations, and the "any" sentinels
    (0 mapId / 0 job) widen correctly.
  - Paging: more listings than the page size → correct page + "more" flag.
  - Expiry: a listing past its TTL is pruned and not returned.
  - (Option B only) round-trip: Register → Load (fresh service) → listing present;
    expired row dropped on Load.

## Notes / gotchas

- **The real bug is the search filter**, not the missing DB. Returning `_listings.Count`
  means every search "succeeds" with a wrong count and no rows — fix this first.
- `Listing` lacks map + owner identity today; the search result packet cannot be built
  without them. Adding those fields is mandatory even for Option A.
- rAthena keeps booking **in RAM** (rebuilt by re-registration) — Option A is the
  parity-correct default. Don't add an EF table unless product explicitly wants durability
  across restarts; if you do, document it as an intentional divergence.
- There are two booking protocol generations in rAthena; match the one the existing C#
  handlers already wire (check `Map.Server/Handlers/` for the booking packet handlers).
