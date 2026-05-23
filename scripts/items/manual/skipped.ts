// Hand-ports for the 27 item scripts that Tools.ItemScriptConvert
// (CONV-2) couldn't translate. The generated buckets list them as
// `// SKIPPED id=...` comments; this file substitutes proper
// registrations.
//
// Categories:
//   • Trivial mutations of currency/counters (`Zeny +=`, `Counter++`)
//     — the converter chokes on top-level assignments to ident-style
//     names; trivial to express directly in TS.
//   • Megaphone items — call `input` + `announce`; item-use has no
//     dialog session so we drop the input prompt and broadcast a
//     placeholder. Future surface: per-item input UI.
//   • Bugged `=` instead of `==` in if-conditions — rAthena's parser
//     swallows it; we restore the obviously-intended `===`.
//   • Pet-egg switch blocks — port the unconditional prefix, leave
//     `getpetinfo(PETINFO_EGGID)` switch as TODO until that surface
//     wires into ItemEquipContext.
//   • Cards with `heal(1-Hp),0;` — paren-wrapped first-arg shape that
//     the parser can't disambiguate; hand-port via PlayerContext.hp.

// ============================================================================
//   Usable items (onUse — async)
// ============================================================================

// Old Blue Box / similar zeny boxes.
registerItem({
    id: 668,
    async onUse(ctx) {
        ctx.player.zeny += ctx.randRange(1000, 10000);
    },
});

registerItem({
    id: 12399,
    async onUse(ctx) {
        ctx.player.zeny += ctx.randRange(50000, 100000);
    },
});

registerItem({
    id: 14508,
    async onUse(ctx) {
        ctx.player.zeny += ctx.randRange(1000, 77777);
    },
});

registerItem({
    id: 22876,
    async onUse(ctx) {
        // Original: `specialeffect2 EF_STEAL;` + zeny gain. Visual-only
        // effect surface isn't on ItemUseContext yet — skip it.
        ctx.player.zeny += ctx.randRange(100, 1000);
    },
});

// Roulette / character-counter bumpers. Permanent character vars use
// PlayerContext.perm (rAthena bare `var`). Need a Number() cast since
// the perm bag stores `number | string`.
function bumpPerm(ctx: ItemUseContext, key: string, by = 1): void {
    const v = Number(ctx.player.perm[key]) || 0;
    ctx.player.perm[key] = v + by;
}

registerItem({ id: 671,  async onUse(ctx) { bumpPerm(ctx, "RouletteGold"); } });
registerItem({ id: 673,  async onUse(ctx) { bumpPerm(ctx, "RouletteBronze"); } });
registerItem({ id: 675,  async onUse(ctx) { bumpPerm(ctx, "RouletteSilver"); } });
registerItem({ id: 22869, async onUse(ctx) { bumpPerm(ctx, "RouletteBronze", 10); } });
registerItem({ id: 12786, async onUse(ctx) { bumpPerm(ctx, "CharMoves"); } });
registerItem({ id: 12790, async onUse(ctx) { bumpPerm(ctx, "CharRename"); } });

// Megaphones — rAthena does:
//   input .@megaphone$;
//   announce strcharinfo(0) + ": " + .@megaphone$, bc_all, 0xFF0000;
// Items don't have a dialog input prompt today. Broadcast a placeholder
// and tag the missing surface in a TODO. AnnounceOpts.color is 0xFF0000.
async function megaphone(ctx: ItemUseContext): Promise<void> {
    // TODO: surface per-item input UI; broadcast the player's typed message.
    await ctx.world.announce(`${ctx.player.name}: (megaphone)`, { color: 0xFF0000 });
}

registerItem({ id: 12221, async onUse(ctx) { await megaphone(ctx); } });
registerItem({ id: 14840, async onUse(ctx) { await megaphone(ctx); } });
registerItem({ id: 23340, async onUse(ctx) { await megaphone(ctx); } });

// McDonald's Ice Cone — daily heal guarded by a permanent var.
// rAthena: `if (gettime(DT_DAYOFMONTH) != MDiceCone) { ... percentheal 50,50; }`
// DT_DAYOFMONTH = 4 in rAthena's date enum.
registerItem({
    id: 12133,
    async onUse(ctx) {
        const today = ctx.world.getTime(4);
        const last = Number(ctx.player.perm.MDiceCone) || 0;
        if (today !== last) {
            ctx.player.perm.MDiceCone = today;
            await ctx.player.percentHeal(50, 50);
        }
    },
});

// Birthday-ish exp scroll — ternary in arg position the parser doesn't handle.
// `getexp (BaseLevel < 100)?15000:1500, 0;`
registerItem({
    id: 23142,
    async onUse(ctx) {
        const base = ctx.player.baseLevel < 100 ? 15000 : 1500;
        await ctx.player.giveExp(base, 0);
    },
});

// ============================================================================
//   Equipment (onEquip / onUnequip — sync)
// ============================================================================

// Spore Eyepatch family — original uses `if (.@b=90)` (assignment, not
// comparison). rAthena treats the assignment as truthy so the branch
// always fires. Port with the obviously-intended `===`.
registerItem({
    id: 2481,
    onEquip(ctx) {
        const a = ctx.readparam("bStr");
        const b = ctx.readparam("bInt");
        if (a >= 90)  ctx.bonus("bBaseAtk", 10);
        if (b === 90) ctx.bonus("bMatkRate", 3);
        if (a >= 120) ctx.bonus("bBaseAtk", 10);
        if (b >= 120) ctx.bonus("bMatkRate", 2);
    },
});

// Same `=` vs `==` bug at two refine breakpoints.
registerItem({
    id: 18924,
    onEquip(ctx) {
        const r = ctx.getrefine();
        ctx.bonus("bDex", 2);
        if (r === 8)  ctx.bonus("bDex", 1);
        if (r === 10) ctx.bonus("bDex", 2);
    },
});

// 470241 / 470242 — same `if (.@r=5)` assignment-as-cond bug. In rAthena
// the inner `if (.@r>=6)` never fires because .@r was just set to 5 by
// the outer assignment; preserve that by gating the inner only on the
// stale `r` value (which is < 6 if r === 5).
registerItem({
    id: 470241,
    onEquip(ctx) {
        const r = ctx.getrefine();
        ctx.bonus("bMaxHPrate", 30);
        ctx.bonus("bMaxSPrate", 30);
        ctx.bonus("bAtkRate", 4 * r);
        if (r === 5) {
            ctx.bonus("bDelayrate", -35);
            if (r >= 6) ctx.bonus("bAspdRate", 10);
        }
    },
});

registerItem({
    id: 470242,
    onEquip(ctx) {
        const r = ctx.getrefine();
        ctx.bonus("bMaxHPrate", 30);
        ctx.bonus("bMaxSPrate", 30);
        ctx.bonus("bMatkRate", 4 * r);
        if (r === 5) {
            ctx.bonus("bDelayrate", -35);
            if (r >= 6) ctx.bonus("bAspdRate", 10);
        }
    },
});

// Doram skill-summed bonus item — long arithmetic, no exotic syntax once
// hand-written.
registerItem({
    id: 480184,
    onEquip(ctx) {
        const a = ctx.getskilllv("SU_SV_STEMSPEAR")
                + ctx.getskilllv("SU_SV_ROOTTWIST")
                + ctx.getskilllv("SU_CN_METEOR")
                + ctx.getskilllv("SU_CN_POWDERING")
                + ctx.getskilllv("SU_CHATTERING")
                + ctx.getskilllv("SU_MEOWMEOW")
                + ctx.getskilllv("SU_NYANGGRASS");
        ctx.bonus("bAspdRate", 5);
        ctx.bonus("bMaxHPrate", 5);
        ctx.bonus2("bSubRace", "RC_Player_Human", 5);
        ctx.bonus2("bSubRace", "RC_Player_Doram", 5);
        ctx.bonus("bSPDrainValue", ctx.getskilllv("SU_CHATTERING"));
        ctx.bonus2("bSkillAtk", "SU_CN_METEOR", 10 * ctx.getskilllv("SU_NYANGGRASS"));
        if (ctx.getskilllv("SU_SPIRITOFLAND") === 1) {
            ctx.bonus("bInt", a);
            ctx.bonus2("bSkillAtk", "SU_SV_STEMSPEAR", a);
        }
    },
});

// HPLoss cards 4263 / 4499 / 300403 — RECOVERED by GAP-2. The parser
// now accepts the `name(expr),more,...;` paren-wrapped-first-arg shape
// rAthena uses in `heal(1-Hp),0;`, so the converter emits both onEquip
// AND onUnequip for these cards. No hand-port needed here.

// Pet-egg-conditional armors. rAthena uses `switch( getpetinfo(PETINFO_EGGID) )`
// to grant different bonuses per egg. ItemEquipContext doesn't surface pet
// info yet — port the always-on prefix bonuses and leave the per-egg
// switch as a TODO until the pet surface wires in.
registerItem({
    id: 15980,
    onEquip(ctx) {
        ctx.bonus2("bAddSize", "Size_All", 5);
        ctx.bonus2("bMagicAddSize", "Size_All", 5);
        // TODO: switch on getpetinfo(PETINFO_EGGID) for per-egg bonuses.
    },
});

registerItem({
    id: 410027,
    onEquip(ctx) {
        ctx.bonus2("bAddSize", "Size_All", 10);
        ctx.bonus2("bMagicAddSize", "Size_All", 10);
        // TODO: switch on getpetinfo(PETINFO_EGGID) for per-egg bonuses.
    },
});

registerItem({
    id: 410028,
    onEquip(ctx) {
        ctx.bonus("bAspdRate", 10);
        // TODO: switch on getpetinfo(PETINFO_EGGID) for per-egg bonuses.
    },
});

registerItem({
    id: 490405,
    onEquip(ctx) {
        ctx.bonus2("bSubRace", "RC_All", 5);
        ctx.bonus2("bSubRace", "RC_Player_Doram", -5);
        ctx.bonus2("bSubRace", "RC_Player_Human", -5);
        ctx.bonus2("bExpAddRace", "RC_All", 5);
        // TODO: switch on getpetinfo(PETINFO_EGGID) for per-egg bonuses.
    },
});
