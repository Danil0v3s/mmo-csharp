// Parses rAthena's declarative npc/re/{warps,mobs,mapflag}/*.txt files
// into SQL seed scripts for the warp / mob_spawn / map_flag tables.
//
// Usage:
//   dotnet run --project Tools.RathenaImporter -- \
//       <rathena-repo-path> <output-dir>
//
// Produces three files in <output-dir>:
//   seed_warps.sql
//   seed_mob_spawns.sql
//   seed_map_flags.sql
//
// Entry-point .conf files walked:
//   npc/re/scripts_warps.conf
//   npc/re/scripts_monsters.conf
//   npc/re/scripts_mapflags.conf
//
// Lines with script bodies (containing `script`, `{`, or `WARPNPC,`)
// are skipped — they need the rAthena script engine to evaluate.

using System.Text;
using Tools.RathenaImporter;

if (args.Length != 2)
{
    Console.Error.WriteLine("usage: dotnet run --project Tools.RathenaImporter -- <rathena-repo> <output-dir>");
    return 1;
}

var rathenaPath = Path.GetFullPath(args[0]);
var outputDir = Path.GetFullPath(args[1]);
if (!Directory.Exists(rathenaPath))
{
    Console.Error.WriteLine($"rAthena repo not found: {rathenaPath}");
    return 1;
}
Directory.CreateDirectory(outputDir);

var nperPath = Path.Combine(rathenaPath, "npc", "re");

var warpFiles = ConfReader.LoadReferencedFiles(Path.Combine(nperPath, "scripts_warps.conf"), rathenaPath);
var mobFiles = ConfReader.LoadReferencedFiles(Path.Combine(nperPath, "scripts_monsters.conf"), rathenaPath);
var flagFiles = ConfReader.LoadReferencedFiles(Path.Combine(nperPath, "scripts_mapflags.conf"), rathenaPath);

Console.WriteLine($"Found {warpFiles.Count} warp files, {mobFiles.Count} mob-spawn files, {flagFiles.Count} mapflag files.");

var warps = warpFiles.SelectMany(WarpParser.ParseFile).ToList();
var spawns = mobFiles.SelectMany(MobSpawnParser.ParseFile).ToList();
var flags = flagFiles.SelectMany(MapFlagParser.ParseFile).ToList();

Console.WriteLine($"Parsed {warps.Count} warps, {spawns.Count} mob spawns, {flags.Count} map flags.");

SqlWriter.WriteWarps(Path.Combine(outputDir, "seed_warps.sql"), warps);
SqlWriter.WriteMobSpawns(Path.Combine(outputDir, "seed_mob_spawns.sql"), spawns);
SqlWriter.WriteMapFlags(Path.Combine(outputDir, "seed_map_flags.sql"), flags);

Console.WriteLine($"Wrote SQL to {outputDir}");
return 0;
