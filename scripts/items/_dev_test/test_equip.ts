// Hand-written test equipment used by CONV-1 to exercise the synchronous
// onEquip path. Mirrors a simple +10 ATK weapon — the kind of item that
// rAthena writes as `bonus bAtk,10;` in item_db.script.

import type { ItemRegistration } from "../../types/api";

export const testKnife: ItemRegistration = {
    id: 999002,
    nameAegis: "_DevTest_Knife",
    nameEnglish: "Dev Test Knife",
    onEquip(ctx) {
        ctx.bonus("bAtk", 10);
    },
    onUnequip(_ctx) {
        // No-op — the bundle is rebuilt from scratch on next recalc.
        // Real items rarely need onUnequip; included here only to prove
        // the hook fires.
    },
};
