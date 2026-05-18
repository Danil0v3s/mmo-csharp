import type { NpcRegistration } from "@server/api";

// Phase 4 demo — exercises the persistence + var-reg surface.
// Numbers next to each menu option survive logout/login on the loop:
//   perm.totalVisits        — per-character, persisted via char_reg_num
//   account.totalAccountVisits  — per-account on this login server (#var)
//   accountGlobal.lifetimeVisits — per-account across login servers (##var)
//   plus zeny / hp mutations that now ALSO survive logout (autosave +
//   final-save write directly to the char row).
export const phase1Test: NpcRegistration = {
    map: "prontera",
    x: 160, y: 160, dir: 4,
    sprite: 105,
    name: "Persistence Probe",
    async onClick(ctx) {
        const p = ctx.player!;

        // Count visits in each persistence scope. The `??` falls back to 0
        // the first time the key is missing (i.e. before this NPC has ever
        // been clicked by this character / account).
        const perChar    = ((p.perm.totalVisits           as number) ?? 0) + 1;
        const perAccount = ((p.account.totalAccountVisits as number) ?? 0) + 1;
        const lifetime   = ((p.accountGlobal.lifetimeVisits as number) ?? 0) + 1;
        p.perm.totalVisits             = perChar;
        p.account.totalAccountVisits   = perAccount;
        p.accountGlobal.lifetimeVisits = lifetime;

        await ctx.mes(`Hello, ${p.name}.`);
        await ctx.mes(`This char has visited me ${perChar} time(s).`);
        await ctx.mes(`Across this account: ${perAccount}.`);
        await ctx.mes(`Lifetime (cross-server): ${lifetime}.`);
        await ctx.mes(`Current zeny: ${p.zeny}    HP ${p.hp}/${p.maxHp}`);
        await ctx.next();

        const choice = await ctx.select([
            "Give 1000 zeny (persists on logout)",
            "Damage me 10 HP (persists on logout)",
            "Set perm.title = \"Champion\"",
            "Show perm.title",
            "Cancel",
        ]);

        if (choice === 1) {
            p.zeny += 1000;
            await ctx.mes(`Zeny: ${p.zeny}. Log out and back in to verify it stuck.`);
        } else if (choice === 2) {
            p.hp = Math.max(1, p.hp - 10);
            await ctx.mes(`HP: ${p.hp}/${p.maxHp}. Log out and back in to verify it stuck.`);
        } else if (choice === 3) {
            p.perm.title = "Champion";
            await ctx.mes(`perm.title = "${p.perm.title}". Stored as a string in char_reg_str.`);
        } else if (choice === 4) {
            const title = p.perm.title as string | undefined;
            await ctx.mes(title ? `perm.title = "${title}"` : `perm.title is unset.`);
        }

        await ctx.close();
    },
};
