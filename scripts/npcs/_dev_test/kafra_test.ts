import type { NpcRegistration } from "@server/api";

// Matches rAthena's canonical kafra flow: mes lines, then a `next` to open
// the dialog window, then a menu. If only this NPC renders (and Phase 1
// Test doesn't), the difference is that ZC_WAIT_DIALOG (0x00b5) is what
// actually opens the client's dialog window.
export const kafraTest: NpcRegistration = {
    map: "prontera",
    x: 165, y: 160, dir: 4,
    sprite: 114,
    name: "Kafra Test",
    *onClick(ctx) {
        yield ctx.mes("[Kafra Employee]");
        yield ctx.mes("Welcome to the Kafra Service.");
        yield ctx.next();
        yield ctx.select(["Save", "Storage", "Cancel"]);
        const choice = ctx.lastSelection;
        if (choice === 1) {
            yield ctx.mes("Your respawn point is set.");
        } else if (choice === 2) {
            yield ctx.mes("Storage is not yet implemented.");
        } else {
            yield ctx.mes("Come back anytime.");
        }
        yield ctx.close();
    },
};
