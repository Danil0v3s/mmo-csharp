import type { NpcRegistration } from "@server/api";

export const kafraTest: NpcRegistration = {
    map: "prontera",
    x: 165, y: 160, dir: 4,
    sprite: 114,
    name: "Kafra Test",
    async onClick(ctx) {
        await ctx.mes("[Kafra Employee]");
        await ctx.mes("Welcome to the Kafra Service.");
        const choice = await ctx.select(["Save", "Storage", "Cancel"]);
        if (choice === 1) {
            await ctx.mes("Your respawn point is set.");
        } else if (choice === 2) {
            await ctx.mes("Storage is not yet implemented.");
        }
        await ctx.close();
    },
};
