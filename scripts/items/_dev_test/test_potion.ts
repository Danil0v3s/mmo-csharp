// Hand-written test item used by CONV-1 to validate the registerItem()
// pathway end-to-end before bulk conversion lands. Mirrors a real Red Potion
// (rAthena item id 501) with a heal-on-use hook.
//
// The bulk converter (Tools.ItemScriptConvert, CONV-2) emits files in the
// sibling generated/ subtree. Files here in _dev_test/ stay hand-edited.
//
// Only `id` is required — every other item-db column (name_aegis,
// name_english, type, weight, slots, …) lives in SQL and is owned by
// IItemCatalog. The registrar is purely for hook attachment by id.

import type { ItemRegistration } from "../../types/api";

export const testPotion: ItemRegistration = {
    id: 999001,
    async onUse(ctx) {
        // Mirrors `itemheal rand(45,65),0;` from item id 501.
        await ctx.player.itemHeal(ctx.randRange(45, 65), 0);
    },
};
