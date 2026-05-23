// _dev_test items — hand-written fixtures that validate the registerItem()
// surface. Real items are generated under scripts/items/generated/ by
// Tools.ItemScriptConvert (CONV-2). Authors who hand-author specific items
// add files alongside these and re-export them through this index.

import { testPotion } from "./test_potion";
import { testKnife } from "./test_equip";

registerItem(testPotion, testKnife);
