// Item subtree entry. Phase 1 included only dev-test fixtures; CONV-2
// adds the generated subtree (rAthena's item_db_* SQL translated to
// .ts via Tools.ItemScriptConvert). Generated files re-run on each
// converter pass; hand-edits go under _dev_test/ or sibling subfolders.

import "./generated";
import "./_dev_test";
