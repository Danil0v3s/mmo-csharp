# Parity Roadmap — Timeline & pick-order (vertical rebuild)

Companion to [README.md](README.md). The README is the **what**; this is the
**in-what-order**. Each ticket is a vertical slice (one playable capability, all layers).
The loop / a contributor takes the **first ticket in pick-order whose `Depends on:` are
all in `done/`**.

Old per-layer sequencing + the full historical progress log are archived in
[`_archive/TIMELINE-history.md`](_archive/TIMELINE-history.md).

## Standing directives

- **Gameplay first.** Phase 2 capabilities are the biggest player-facing win and where
  "done ≠ playable" hurt most.
- **Combat last, scripting truly last** (user pivot, persisted in memory).
- **Each ✅ must be playable end-to-end and survive logout** — not "service exists".

## Phase order

```
Phase A  gameplay/   ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓   playable capabilities (16)   ← do these
Phase B  infra/      ▓▓▓▓▓▓▓▓             small vertical features (8)  ← parallel anytime
Phase B  mobai/      ▓▓                   mob AI (2)                   ← parallel anytime
Phase C  status/     ▓▓▓▓                 SC depth (4)
Phase D  skills/     ▓▓▓▓▓▓▓▓▓            per-family depth (9)
Phase E  combat/     ▓▓▓▓▓                damage formula tail (5)      ← combat last
Phase F  scripting/  ▓▓▓▓▓▓▓              NPC runtime + content (7)    ← truly last
```

## Pick-order (Phase A — gameplay, do top-to-bottom)

Ordered by leverage (most-used capabilities + dependency-free first):

1. **GP-MAIL** — most-used social feature; the mail service already has real transfer logic, needs the RODEX packet set + persistence round-trip. Dependency-free.
2. **GP-PARTY** — service mostly exists; pure packet-bridge work. Dependency-free.
3. **GP-QUEST** — drives most PvE content; quest service real, needs load-on-enter + objective filters + UI packets. Dependency-free.
4. **GP-PET** — popular; entity + catch/hatch landed, needs packets + combat + persistence. Dependency-free.
5. **GP-VEND** — player economy; transfer logic landed, needs packets + autotrade persistence. Dependency-free.
6. **GP-BUYSTORE** — pairs with vending. Dependency-free.
7. **GP-CASHSHOP** — buy path landed, needs catalog data + point persistence + UI packets. Dependency-free.
8. **GP-AUCTION** — economy; map-side wiring landed, needs packets + item fidelity. Dependency-free.
9. **GP-ACHIEVE** — pairs with quest UI packets. Dependency-free (shares PACKET work with GP-QUEST — coordinate).
10. **GP-GUILD** — large packet set; service exists. Dependency-free.
11. **GP-HOMUN** — entity slice landed, needs AI/combat/growth/hunger/packets. Dependency-free.
12. **GP-MERC** — entity slice landed, needs AI/combat/lifetime/packets. Dependency-free.
13. **GP-ELEM** — lifetime sweep landed, needs AI + create/load/delete IPC. Dependency-free.
14. **GP-INSTANCE** — lifecycle landed; **must build the dynamic-map subsystem** (the hard prerequisite) before instances are enterable. Largest gameplay ticket.
15. **GP-WOE** — scheduler landed; needs castle Emperium/guardian content + can-hit gate. Soft-depends on GP-GUILD (castle ownership) + GP-INSTANCE patterns.
16. **GP-MVPFAME** — MVP reward packets + fame ranking board. Soft-depends on GP-PARTY (kill credit fan-out).

Phase B (infra + mobai) can be pulled **in parallel** any time a contributor wants a
smaller, self-contained vertical — none of them block Phase A.

Then Phase C (status) → D (skills) → E (combat) → F (scripting), per the standing
directive. Within each, take the first dependency-free row; `SK-ENGINE` should lead the
skills phase (it unblocks the family tickets).

## Progress log

Update as cards move (date · ticket · todo→inprogress / inprogress→done · one line).

- **2026-06-04** — **GP-CASHSHOP** inprogress→done (Phase A, 2 turns). Built the cash-shop client packet bridge + data + account-bound point persistence (the BuyList purchase engine was archive FEATURE-13). Turn 1 — data: rAthena ships db/item_cash.yml empty (admins fill db/import/), so the importer now falls back to a project default catalog of real items (7 tabs, 11 rows) regenerating seed_item_cash.sql; packets (11 new): CZ open/list/close/buy (0x0b6d/0x08c9/0x084a/0x0848) + ZC open-balances/scheduler-list/buy-result (0x0a2b/0x08ca/0x0849, CASHSHOP_RESULT_*) + sale selling/amount (0x09b2/0x09c4); ICashShopClientService emit hub, CatalogTabs()/ActiveSaleNotifications() accessors, SaleNotifyLogin emit, Open/List/Close/Buy handlers (trading gate + kafra-cash split). Turn 2 — persistence: #CASHPOINTS/#KAFRAPOINTS are acc_reg_num registers (rAthena pc_readaccountreg/pc_setaccountreg), so no new proto/table — new CashPointsReg helper (mirrors DieCounterReg) hydrates on map-enter (NotifyActorInitHandler) + mirrors live balances on save (PlayerStateService), riding the existing account var-reg pipeline. A player opens the cash shop, browses tabs, buys with a kafra-then-cash split (item granted, success ack, sale-tab discounted price), is rejected on insufficient points/full inventory, and relogs with the remaining balance intact (account-bound). 25 cash-shop-suite tests (19 bridge/service + 6 persistence); full suite 4516 pass (1 standing replay-fixture). Filed GP-CASHSHOP-SLOT-WEIGHT-CODE (slot-vs-weight reject code) + GP-CASHSHOP-SALE-BANNER (timed-sale @sale/persistence/refresh subsystem).
- **2026-06-04** — **GP-BUYSTORE** inprogress→done (Phase A, 2 turns). Built the buying-store client packet bridge (the escrow/transfer/refund service was archive FEATURE-12): turn 1 — open/create (CZ_REQ_OPEN_BUYING_STORE, escrow the buyer's zeny up to the limit) + close-refund + owner item list (ZC_MYITEMLIST_BUYING_STORE) + stall sign on-map (ZC_BUYING_STORE_ENTRY, AOI) + open-fail + disappear + escrow/refund par-change. Turn 2 — click-to-view (CZ_REQ_CLICK_TO_BUYING_STORE → ZC_ACK_ITEMLIST_BUYING_STORE visitor offer list) + sell-in/trade (CZ_REQ_TRADE_BUYING_STORE → buyingstore_trade: item→buyer, escrow→seller; rAthena gates store-id→DealFailed / unwanted+overcount→OverCount / escrow-short→BuyerLacksZeny / buyer-full→silent; per-item seller delete + buyer amount-update + buyer pickup + seller SP_ZENY; auto-close+refund when escrow exhausted or all offers filled). A player opens a buying store paying Nz for an item, escrowing the limit; others see the stall, click to view its offers, and sell matching items in (paid from the held escrow); closing refunds the remainder. 16 buyingstore-suite tests; full suite 4501 pass (1 standing replay-fixture). Search ➡️ INF-SEARCHSTORE (universal market search, shared with GP-VEND); autotrade ➡️ GP-AUTOTRADE-RUNTIME.
- **2026-06-03** — Board restructured from layer-sliced to vertical. Old board (112 todo
  + 119 done) archived under `_archive/`; new TEMPLATE enforces end-to-end scope. The
  landed code from the archived `FEATURE-01..15` / `COMBAT-01..96` / `SC-01..08` etc.
  remains in the repo and is cited by the vertical tickets as "verify-and-extend".
- **2026-06-03** — **GP-MAIL** inprogress→done (Phase A, 6 turns). Built the entire RODEX client packet bridge to rAthena struct fidelity + handler unit tests (the service + persistence IPC were already built in archive FEATURE-05): receive side (open-mailbox/refresh→ZC_ACK_MAIL_LIST, read→ZC_ACK_READ_RODEX, zeny/item-from-mail→acks, delete→ack) + compose side (begin-write/check-name/add-item/remove-item/send→acks) + the service methods (RequestInbox/Read/Delete/CheckReceiver) + the overweight gate. A player can compose a mail with zeny+a carded item, send it, and the recipient opens RODEX, reads, claims (cards intact), and deletes it — all client→service→client. 16 handler tests + extended service tests; full suite 4414 pass (1 standing replay-fixture). Filed GP-MAIL-RENTAL (rental expiry) + GP-MAIL-PARTIAL-CLAIM (separated claims); live-client wire validation is the project's standing deferred pass.
- **2026-06-03** — **GP-PARTY** inprogress→done (Phase A, 3 turns). Built the party client packet bridge (the char IPC + notify layer + cache were already present): create (CZ_MAKE_GROUP) + invite-by-name (CZ_PARTY_JOIN_REQ) + leave/expel/change-leader/change-option handlers (driving the established IIntifService path) + the HP-bar/minimap-dot sync (new ZC_NOTIFY_HP_TO_GROUPM + PartySyncService, rAthena party_send_xy_timer cadence, wired into MapServerImpl). A player can create/invite/accept/leave/expel/change-leader/set-EXP-share and see teammates' HP bars + dots. 19 party-suite tests; full suite 4429 pass (1 standing replay-fixture). Filed GP-PARTY-EXPEL-REASON + GP-PARTY-INSTANT-HP.
- **2026-06-04** — **GP-VEND** inprogress→done (Phase A, 3 turns). Built the vending client packet bridge (the buyer↔vendor transfer was archive FEATURE-11): open from cart (CZ_REQ_OPENSTORE2, offers validated against the live cart) → the vendor's own item list (ZC_PC_PURCHASE_MYITEMLIST) + stall sign on-map (ZC_STORE_ENTRY, AOI) + open ack; browse (CZ_REQ_VENDING_ITEMS → ZC_PC_PURCHASE_ITEMLIST_FROMMC price list + vended-id anti-desync); buy (CZ_PC_PURCHASE_ITEMLIST_FROMMC → PurchaseReq with the rAthena result codes + vendor sale report + buyer item-pickup + SP_ZENY par-change for both); sold-out auto-close + stall disappear. A player opens a shop from their cart, others see the stall, a buyer browses + buys, and the shop auto-closes when sold out. ~18 vending-suite tests; full suite green (1 standing replay-fixture). Filed GP-VEND-OVERWEIGHT (weight gate) + GP-AUTOTRADE-RUNTIME (the offline-shop headless runtime, shared with GP-BUYSTORE).
- **2026-06-04** — **GP-PET** inprogress→done (Phase A, 7 turns). Built the entire pet client packet bridge + persistence round-trip (the service/entity layer was archive FEATURE-07): pet-menu (CZ_COMMAND_PET) + status/changestate emits, the real click-to-tame capture (CZ_TRYCAPTURE → pet_catch_process_end with the live-HP% rate — parity fix off the prior mob-death model) + roulette/start-capture, the egg-list hatch (IT_PETEGG use → ZC_PETEGG_LIST → CZ_SELECT_PETEGG → BirthProcess), rename (CZ_RENAME_PET) + emotion (CZ_PET_ACT/ZC_PET_ACT), the looter-pet loot bag (SummonAiService pet_ai_sub_hard loot branch + pet_lootitem_drop deposit), and the pet_id↔egg-card persistence round-trip (catch→PetCreate→bound egg via CARD0_PET + card-aware GiveItemWithCards; hatch→PetLoad→hydrate intimacy/hunger/name). Fixed a bug where catching created the char pet row but never granted the player an egg. A player tames a mob, hatches the bound egg into the saved pet, feeds/renames/emotes/loots/returns it, and relogs to re-hatch the same pet. ~40 pet-suite tests; full suite 4480 pass (1 standing replay-fixture). Filed GP-PET-CATCH-GATES, GP-PET-RENAME-NAMEPKT, GP-PET-LOOT-OVERFLOW, GP-PET-AUTOSKILL (scripting), GP-PET-LOGIN-RESUMMON, GP-PET-LOYALTY-BONUS (scripting).
- **2026-06-03** — **GP-QUEST** inprogress→done (Phase A, 5 turns). Built the quest client packet bridge + load-on-enter + filters (the quest service state-machine + persistence IPC were archive FEATURE-03/02): ZC_DEL_QUEST/ZC_UPDATE_MISSION_HUNT (live hunt counter) → ZC_ADD_QUEST/+MISSION (quest appears) → ZC_ALL_QUEST_LIST login snapshot + load-on-enter (IIntifService.QuestRequestAsync → Hydrate → PcLogin at LoadEndAck) → CZ/ZC_ACTIVE_QUEST toggle + immediate-save (IQuestSaveTrigger, chrif_save parity) → FEATURE-21 any-mob objective filters (QuestDbEntity +21 filter cols + migration + importer/seed regen of 4826 quests; UpdateMobObjective(QuestMobContext) runs rAthena's 7-check race/size/element/level/location/allow-list gate). A player accepts a quest, sees it with live 0/N progress that ticks on kills (incl. "kill N Fish-type" filter quests), completes it, toggles tracking, and relogs mid-hunt with progress intact. 38 quest-suite tests; full suite 4444 pass (1 standing replay-fixture). Filed GP-QUEST-FILTER-INSTANCE + GP-QUEST-FILTER-DISPLAY.
