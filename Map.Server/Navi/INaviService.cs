namespace Map.Server.Navi;

/// <summary>
/// Navigation file generator. Canonical entry points for rAthena
/// <c>navi.cpp</c> (655 lines, 17 functions) — produces the binary
/// /Navi files the client uses for in-game GPS routes between maps,
/// NPCs, monsters, and warps.
///
/// rAthena's navi generator runs at server boot and writes
/// pre-computed distance matrices to text files. The C# port hasn't
/// shipped the generator yet; this service holds the canonical entry
/// points so the `@navi_generate` GM command + script `navigate_to`
/// BUILTIN have somewhere to land.
/// </summary>
public interface INaviService
{
    /// <summary>rAthena <c>navi_create_lists</c> — entry: produce all .nav files.</summary>
    bool CreateLists(string outputDirectory);

    /// <summary>rAthena <c>navi_path_search</c>.</summary>
    bool PathSearch(string fromMap, short fx, short fy, string toMap, short tx, short ty);

    /// <summary>rAthena <c>map_type</c> — classify a map for the nav file.</summary>
    int MapType(string mapName);

    /// <summary>rAthena <c>fileExists</c>.</summary>
    bool FileExists(string path);

    // The write_* helpers are file-emit primitives — entries reserved
    // for the generator's internal use once it ports.
    void WriteHeader(string path);
    void WriteFooter(string path);
    void WriteMapHeader(string path);
    void WriteMap(string path, string mapName);
    void WriteMapDistance(string path, string fromMap, string toMap, int distance);
    void WriteMapDistances(string path);
    void WriteMapDistHeader(string path);
    void WriteNpc(string path, string mapName, string npcName);
    void WriteNpcDistance(string path, string fromMap, string toMap, int distance);
    void WriteNpcDistances(string path);
    void WriteObjectLists(string path);
    void WriteSpawn(string path, string mapName, int mobId);
    void WriteWarp(string path, string fromMap, string toMap);
}
