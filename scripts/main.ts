// Single entry point loaded by the C# scripting host (Map.Server/Scripting/ScriptHost.cs).
// Side-effect imports walk the rest of the tree; every register*() call below
// the import graph runs at module-evaluation time and populates INpcRegistry.
//
// To add new content: drop a file under the appropriate subtree and add a
// matching `import "./newfile";` line in the nearest index.ts. No C#-side
// changes required.

import "./npcs";
import "./shops";
import "./warps";
import "./spawns";
