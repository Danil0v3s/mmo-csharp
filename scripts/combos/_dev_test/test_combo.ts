// Hand-written test combo used by CONV-1 to validate the registerCombo()
// pathway. Members reference the two _dev_test items so this fires when
// a player simultaneously equips the test potion (id 999001) and test
// knife (id 999002) — which is contrived, since potions aren't equippable;
// the combo's role here is purely registration smoke.
//
// Real combos land via Tools.ItemScriptConvert under combos/generated/.

import type { ComboRegistration } from "../../types/api";

export const testCombo: ComboRegistration = {
    comboId: 999001,
    members: ["_DevTest_Potion", "_DevTest_Knife"],
    onActive(ctx) {
        ctx.bonus("bMaxHPrate", 5);
    },
};
