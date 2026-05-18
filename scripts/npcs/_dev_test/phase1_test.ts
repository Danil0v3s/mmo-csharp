import type { NpcRegistration } from "@server/api";

export const phase1Test: NpcRegistration = {
    map: "prontera",
    x: 160, y: 160, dir: 4,
    sprite: 105,
    name: "Phase 1 Test",
    async onClick(ctx) {
        await ctx.mes("If you can read this, Phase 2 is wired up.");
        await ctx.mes("In Phase 1 you should never see this dialog.");
        await ctx.close();
    },
};
