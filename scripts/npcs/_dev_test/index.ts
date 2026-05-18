// Phase 1 acceptance fixtures aggregated. Each NPC lives in its own file
// as a pure `export const`; this index imports them and makes a single
// register*() call per kind. The varargs registrars mean a directory like
// this — many `export const` files + one index — is the idiomatic shape
// for any content area (cities, quests, jobs, …).

import { phase1Test } from "./phase1_test";
import { kafraTest } from "./kafra_test";
import { eventManager } from "./event_manager";

registerNpc(phase1Test, kafraTest);
registerFloatingNpc(eventManager);
