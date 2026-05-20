using Microsoft.Extensions.Logging;

namespace Map.Server.Navi;

/// <summary>
/// Default <see cref="INaviService"/>. No-op until the generator
/// ports — the entry points are here so the GM-command tier
/// (`@navi_generate`) has a service to invoke.
/// </summary>
public sealed class NaviService : INaviService
{
    private readonly ILogger<NaviService> _logger;
    public NaviService(ILogger<NaviService> logger) => _logger = logger;

    public bool CreateLists(string outputDirectory)
    {
        _logger.LogInformation("navi_create_lists: data-pending (generator not yet ported)");
        return false;
    }
    public bool PathSearch(string fromMap, short fx, short fy, string toMap, short tx, short ty) => false;
    public int MapType(string mapName) => 0;
    public bool FileExists(string path) => System.IO.File.Exists(path);

    public void WriteHeader(string path) { }
    public void WriteFooter(string path) { }
    public void WriteMapHeader(string path) { }
    public void WriteMap(string path, string mapName) { }
    public void WriteMapDistance(string path, string fromMap, string toMap, int distance) { }
    public void WriteMapDistances(string path) { }
    public void WriteMapDistHeader(string path) { }
    public void WriteNpc(string path, string mapName, string npcName) { }
    public void WriteNpcDistance(string path, string fromMap, string toMap, int distance) { }
    public void WriteNpcDistances(string path) { }
    public void WriteObjectLists(string path) { }
    public void WriteSpawn(string path, string mapName, int mobId) { }
    public void WriteWarp(string path, string fromMap, string toMap) { }
}
