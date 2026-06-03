# FEATURE-38 — Cash-point currency persistence (#CASHPOINTS / #KAFRAPOINTS)

> **Epic:** Gameplay-Shop · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** FEATURE-13 · **Blocks:** none

## Problem

`PlayerEntity.CashPoints` / `KafraPoints` are **in-memory only**: they are never
loaded from the DB on login, never saved on logout, and have no proto/IPC field.
They initialise to `0`, so even after FEATURE-13's real `cashshop_buylist` and the
`@cash` / `@points` GM commands, **the balance is lost on logout** and a fresh
login always shows 0 cash. A player cannot accumulate or spend a persistent cash
balance — the cash shop only "works" within a single session after a GM grant.

## Current state (C#)

- `Map.Server/Entities/PlayerEntity.cs:176,178` — `CashPoints` / `KafraPoints` (`int`), in-memory.
- `Map.Server/Shop/Cash/CashShopService.cs:TryPayCash` — debits both pools (FEATURE-13).
- `Map.Server/Status/PlayerLifecycleHelpers.cs:223` `PayCash` — same debit, also in-memory.
- `Map.Server/Gm/Commands/AtCWaveCommands.cs:987,1000` — `@cash` / `@points` mutate them in-memory.
- **No load path** (nothing reads them out of the DB into `PlayerEntity` on connect).
- **No save path** (nothing writes them back on logout / save tick).
- **No proto field** (`char_service.proto` has no cash/kafra in the character-data message).

## rAthena reference (source of truth)

- rAthena stores cash points as **account registry vars**: `#CASHPOINTS` and `#KAFRAPOINTS`
  (`pc.cpp` `pc_paycash` / `pc_getcash` via `pc_setaccountreg` / `pc_readaccountreg`).
- They are **account-bound** (shared across all chars on the account), not char-bound — persisted on
  the login/account side, loaded into `sd->cashPoints` / `sd->kafraPoints` at auth.

## Scope — every sub-system that must be touched

- [ ] Decide storage: account-bound (matches rAthena `#CASHPOINTS`) on the login/account entity, vs.
      char-bound. Prefer account-bound — add columns to the account/registry table (EF migration) or a
      `acc_reg_num`-style row.
- [ ] DB load: populate `PlayerEntity.CashPoints` / `KafraPoints` on connect (char→login IPC fetch, or
      include in the existing account-data load).
- [ ] DB save: persist the balances on logout / save tick / immediately after a buy (so a crash
      doesn't dupe points).
- [ ] Proto/IPC: add the cash/kafra fields to the relevant message (`char_service.proto` or
      `login_service.proto`) + the char/login RPC that round-trips them.
- [ ] Wire `CashShopService.TryPayCash` (and `PlayerLifecycleHelpers.PayCash`, `@cash`, `@points`) to
      trigger the persist so debits/grants survive logout.

## Done criteria

- A player granted cash points, who buys an item, then logs out and back in, sees the **correct
  remaining balance** (debit persisted).
- Balances are account-bound (a second char on the same account sees the same pool) — matching rAthena.
- No in-memory-only currency: the load + save paths both exist and round-trip.

## Test plan

- Repository/IPC round-trip test: set balance, save, reload → same value.
- Regression: buy → balance persisted; relog → balance intact.

## Notes / gotchas

- rAthena caps both at `INT_MAX` (`MAX_CASHPOINT` / `MAX_KAFRAPOINT`).
- Account-bound storage means the save must go through the login/account side, not the char row —
  follow the existing account-data IPC seam.
