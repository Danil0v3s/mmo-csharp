import type { NpcRegistration } from "@server/api";

// Minimal probe — just one mes line + close. If this dialog doesn't render,
// the issue is in ZC_SAY_DIALOG (0x00b4) itself.
export const phase1Test: NpcRegistration = {
    map: "prontera",
    x: 160, y: 160, dir: 4,
    sprite: 105,
    name: "Phase 1 Test",
    *onClick(ctx) {
        yield ctx.mes("Single mes line, then close.");
        yield ctx.close();
    },
};
