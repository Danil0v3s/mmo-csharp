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
// to grant different bonuses per egg. GAP-1 added getpetinfo() to
// ScriptedBonusHost (resolves against PetEntity.EggId via IEntityRegistry),
// so the per-egg switches now port cleanly. The host accepts either the
// rAthena enum string or the integer constant — we use the string for
// readability.

// Wonder Egg Basket (id 15980) — db/re/item_db_equip.yml Id: 15980
registerItem({
    id: 15980,
    onEquip(ctx) {
        ctx.bonus2("bAddSize", "Size_All", 5);
        ctx.bonus2("bMagicAddSize", "Size_All", 5);
        const egg = Number(ctx.getpetinfo("PETINFO_EGGID")) || 0;
        switch (egg) {
            case 9121: // Ork_Hero_EGG
                ctx.bonus2("bAddClass", "Class_Boss", 10);
                break;
            case 9115: // Bacsojin_Egg2
                ctx.skill("AB_RENOVATIO", 4);
                break;
            case 9113: // Roost_Of_Skelion
                ctx.bonus2("bAddItemHealRate", 579, 333);
                break;
            case 9088: { // Angeling_Egg
                const lukDiv3 = Math.floor(ctx.readparam("Luk") / 3);
                ctx.bonus2("bExpAddRace", "RC_All", 10);
                ctx.bonus("bBaseAtk", lukDiv3);
                ctx.bonus("bMatk", lukDiv3);
                break;
            }
            case 9087: // High_Orc_Egg
                ctx.bonus2("bAddRace", "RC_Demon", 10);
                break;
            case 9055: // Succubus_Egg
                ctx.bonus2("bSPDrainRate", 10, 1);
                break;
            case 9052: // Incubus_Egg
                ctx.bonus2("bHPDrainRate", 20, 5);
                break;
            case 9119: // Alicel_EGG
                ctx.bonus("bVariableCastrate", -10);
                ctx.bonus2("bMagicAtkEle", "Ele_Neutral", 5);
                break;
        }
    },
});

// Wonder Egg Basket (id 410027) — db/re/item_db_equip.yml Id: 410027
registerItem({
    id: 410027,
    onEquip(ctx) {
        ctx.bonus2("bAddSize", "Size_All", 10);
        ctx.bonus2("bMagicAddSize", "Size_All", 10);
        const egg = Number(ctx.getpetinfo("PETINFO_EGGID")) || 0;
        switch (egg) {
            case 9112: // Moonlight_Egg
                ctx.bonus2("bHPVanishRate", 40, 4);
                break;
            case 9088: { // Angeling_Egg
                const luk = ctx.readparam("Luk");
                ctx.bonus("bBaseAtk", luk);
                ctx.bonus("bMatk", luk);
                ctx.bonus2("bExpAddClass", "Class_All", 5);
                break;
            }
            case 9096: // Cat_O_Nine_Tail_Egg
                ctx.bonus2("bAddRace", "RC_Demon", 30);
                ctx.bonus2("bMagicAddRace", "RC_Demon", 30);
                ctx.bonus2("bSubRace", "RC_Demon", 5);
                break;
            case 9087: // High_Orc_Egg
                ctx.bonus2("bAddRace", "RC_Brute", 30);
                ctx.bonus2("bMagicAddRace", "RC_Brute", 30);
                ctx.bonus2("bSubRace", "RC_Brute", 5);
                break;
            case 9069: // Mastering_Egg
                ctx.bonus2("bAddRace", "RC_Plant", 30);
                ctx.bonus2("bMagicAddRace", "RC_Plant", 30);
                ctx.bonus2("bSubRace", "RC_Plant", 5);
                break;
            case 9106: // Metaller_Egg
                ctx.bonus3("bAutoSpell", "WM_METALICSOUND", 5, 150);
                break;
        }
    },
});

// Wonder Egg Basket (id 410028) — db/re/item_db_equip.yml Id: 410028
// rAthena groups multiple eggs per case via `case X: case Y: ...; break;`.
// JS switch fall-through handles the same pattern naturally.
registerItem({
    id: 410028,
    onEquip(ctx) {
        ctx.bonus("bAspdRate", 10);
        const egg = Number(ctx.getpetinfo("PETINFO_EGGID")) || 0;
        switch (egg) {
            case 9109: // Sweet_Drops_Egg
            case 9112: // Moonlight_Egg
            case 9115: // Bacsojin_Egg2
            case 9121: // Orc_Hero_Egg_
            case 9126: // Kiel_Egg
            case 9136: // Eddga_Egg
                ctx.bonus("bBaseAtk", 200);
                ctx.bonus("bMatk", 200);
                ctx.bonus("bAllStats", 10);
                break;
            case 9088: // Angeling_Egg
            case 9108: // Xm_Teddybear_Egg
            case 9113: // Roost_Of_Skelion
                ctx.bonus("bBaseAtk", 200);
                ctx.bonus("bMatk", 200);
                ctx.bonus2("bAddSize", "Size_All", 10);
                ctx.bonus2("bMagicAddSize", "Size_All", 10);
                break;
            case 9069: // Mastering_Egg
            case 9087: // High_Orc_Egg
            case 9096: // Cat_O_Nine_Tail_Egg
            case 9106: // Metaller_Egg
            case 9117: // Contaminated_Wanderer_Egg
            case 9118: // Aliot_Egg
            case 9119: // Alicel_Egg
            case 9120: // Aliza_Egg
            case 9124: // Ep17_2_C_Admin2_Egg
                ctx.bonus("bBaseAtk", 200);
                ctx.bonus("bMatk", 200);
                ctx.bonus("bDef", 150);
                ctx.bonus("bMdef", 15);
                break;
        }
    },
});

// Beast Rings (id 490405) — db/re/item_db_equip.yml Id: 490405
registerItem({
    id: 490405,
    onEquip(ctx) {
        ctx.bonus2("bSubRace", "RC_All", 5);
        ctx.bonus2("bSubRace", "RC_Player_Doram", -5);
        ctx.bonus2("bSubRace", "RC_Player_Human", -5);
        ctx.bonus2("bExpAddRace", "RC_All", 5);
        const egg = Number(ctx.getpetinfo("PETINFO_EGGID")) || 0;
        switch (egg) {
            case 9003: // Poporing_Egg
                ctx.bonus2("bSubRace", "RC_Plant", 5);
                ctx.bonus2("bExpAddRace", "RC_All", 15);
                break;
            case 9040: // Civil_Servant_Egg
                ctx.bonus2("bSubRace", "RC_Angel", 5);
                ctx.bonus2("bExpAddRace", "RC_All", 15);
                break;
            case 9015: // Smokie_Egg
                ctx.bonus2("bSubRace", "RC_Brute", 5);
                ctx.bonus2("bExpAddRace", "RC_All", 15);
                break;
            case 9020: // Sohee_Egg
                ctx.bonus2("bSubRace", "RC_Demon", 5);
                ctx.bonus2("bExpAddRace", "RC_All", 15);
                break;
            case 9046: // Goblin_Leader_Egg
                ctx.bonus2("bSubRace", "RC_Dragon", 5);
                ctx.bonus2("bExpAddRace", "RC_All", 15);
                break;
            case 9044: // Shinobi_Egg
                ctx.bonus2("bSubRace", "RC_DemiHuman", 5);
                ctx.bonus2("bExpAddRace", "RC_All", 15);
                break;
            case 9007: // Steel_Chonchon_Egg
                ctx.bonus2("bSubRace", "RC_Insect", 5);
                ctx.bonus2("bExpAddRace", "RC_All", 15);
                break;
            case 9018: // Munak_Egg
                ctx.bonus2("bSubRace", "RC_Undead", 5);
                ctx.bonus2("bExpAddRace", "RC_All", 15);
                break;
            case 9050: // Medusa_Egg
                ctx.bonus2("bSubRace", "RC_Formless", 5);
                ctx.bonus2("bExpAddRace", "RC_All", 15);
                break;
            case 9062: // Novice_Poring_Egg
                ctx.bonus2("bSubRace", "RC_Fish", 5);
                ctx.bonus2("bExpAddRace", "RC_All", 15);
                break;
        }
    },
});
