import type { NpcRegistration } from "@server/api";

// Exercises the Phase 3 player surface: read identity / stats, mutate zeny,
// heal, and use the session-scope variable bag.
export const phase1Test: NpcRegistration = {
    map: "prontera",
    x: 160, y: 160, dir: 4,
    sprite: 105,
    name: "Player Probe",
    async onClick(ctx) {
        const p = ctx.player!;
        await ctx.mes(`Hello, ${p.name}!`);
        await ctx.mes(`Base Lv ${p.baseLevel} / Job Lv ${p.jobLevel}`);
        await ctx.mes(`HP ${p.hp} / ${p.maxHp}    SP ${p.sp} / ${p.maxSp}`);
        await ctx.mes(`Zeny: ${p.zeny}`);
        await ctx.mes(`STR ${p.str}  AGI ${p.agi}  VIT ${p.vit}  INT ${p.int}  DEX ${p.dex}  LUK ${p.luk}`);
        await ctx.next();

        const choice = await ctx.select([
            "Heal me",
            "Give 100 zeny",
            "Take 100 zeny",
            "Show session var",
            "Cancel",
        ]);

        if (choice === 1) {
            await p.heal(p.maxHp - p.hp, p.maxSp - p.sp);
            await ctx.mes("Restored to full.");
        } else if (choice === 2) {
            p.zeny += 100;
            await ctx.mes(`Now: ${p.zeny} zeny.`);
        } else if (choice === 3) {
            p.zeny -= 100;
            await ctx.mes(`Now: ${p.zeny} zeny.`);
        } else if (choice === 4) {
            const visits = ((p.session.visits as number | undefined) ?? 0) + 1;
            p.session.visits = visits;
            await ctx.mes(`Visits this session: ${visits}`);
        }

        await ctx.close();
    },
};
