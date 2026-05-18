// Phase 1 acceptance fixtures. Three NPCs at prontera near the default spawn
// (150, 150) — close enough to be in the AOI of a freshly-spawned player so
// the visibility path is exercised on first login. Click handlers are written
// in the Phase-2-ready style; until Phase 2 lands, Map.Server logs the click
// and sends ZC_CLOSE_DIALOG without invoking the closure.

registerNpc({
    map: "prontera",
    x: 160, y: 160, dir: 4,
    sprite: 105,
    name: "Phase 1 Test",
    async onClick(ctx) {
        await ctx.mes("If you can read this, Phase 2 is wired up.");
        await ctx.mes("In Phase 1 you should never see this dialog.");
        await ctx.close();
    },
});

registerNpc({
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
});

registerFloatingNpc({
    name: "EventManager",
    onInit() {
        // Phase 5 will actually invoke this. For now it just proves the
        // floating-NPC registration path works.
    },
});
