# GP-ACHIEVE-REWARD-SCRIPT — Scripted achievement rewards run on claim

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** SCR-PLAYER (the reward `Script` runtime) · **Unlocks:** none

## The deliverable (definition of done, in one sentence)

> When a player claims an achievement whose reward carries a **`Script`** (e.g. `getexp`,
> `sc_start`, a buff), that script **runs** as part of the reward grant — not just the item + title.

## Player story / why it matters

GP-ACHIEVE landed the manual reward claim: on `CZ_REQ_ACH_REWARD` the service grants the reward **item**
(by aegis name + amount) and the **title**, stamps `RewardedUnix` (idempotent), and emits the proper
client acks (success/list-resend on title). rAthena's `achievement_get_reward` additionally
`run_script(adb->rewards.script, …)` (achievement.cpp:687) — the optional reward **script** (the YAML
`Rewards: { Script: "..." }` block). The current port grants item + title fully but skips the script,
because there is no script runtime yet.

## Current state — per layer

| Layer | Exists? | Where / what's missing |
|---|---|---|
| Item + title reward | ✅ | `AchievementService.GetReward` grants item (`IInventoryService.GiveItem`) + stamps title. |
| Reward Script | ❌ | `AchievementDbEntity` has no `RewardScript` column; `GetReward` has no script hook. |

## rAthena reference

- `src/map/achievement.cpp:687` — `run_script(adb->rewards.script, 0, sd->bl.id, fake_nd->bl.id);` inside
  `achievement_get_reward`.
- `src/map/achievement.cpp` parse — the `Rewards: { Item, Amount, Script, TitleID }` mapping.

## Scope — every layer

- [ ] **Data**: `AchievementDbEntity.RewardScript` column + EF migration; importer carries the YAML
  `Rewards.Script`; seed regen.
- [ ] **Service**: in `AchievementService.GetReward`, after the item/title grant, run the reward script
  via the SCR-PLAYER runtime bound to the claiming PC (before stamping `RewardedUnix`, matching rAthena
  order). Keep idempotency (a re-claim still no-ops).
- [ ] **Tests**: a reward `Script` that grants exp / starts an SC runs exactly once on claim.

## Done criteria

- Claiming an achievement with a reward script executes the script (observable effect: exp gained / SC
  applied) exactly once.
- Item + title rewards still work; the claim stays idempotent.

## Notes / gotchas

- The hook is a single `run_script` call in `GetReward` — small once SCR-PLAYER exists; do not stub it
  before then (leave the item/title path intact).
