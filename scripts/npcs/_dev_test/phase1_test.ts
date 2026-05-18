import type { NpcRegistration } from "@server/api";

export const phase1Test: NpcRegistration = {
    map: "prontera",
    x: 160, y: 160, dir: 4,
    sprite: 105,
    name: "Phase 1 Test",
    async onClick(ctx) {
        await ctx.mes("Phase 2 (ClearScript V8) — native async/await.");
        await ctx.mes("Three lines of dialog, then a Next button.");
        await ctx.next();
        await ctx.mes("You clicked Next. Now close to finish.");
        await ctx.close();
    },
};
