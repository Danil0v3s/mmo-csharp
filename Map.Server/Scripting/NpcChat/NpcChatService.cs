using System.Text.RegularExpressions;
using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Scripting.NpcChat;

/// <summary>
/// Default <see cref="INpcChatService"/>. Per-NPC pattern store keyed
/// by set id; CheckChat walks active sets and returns the first match.
/// Pattern compilation happens at registration time (regex caching).
/// </summary>
public sealed class NpcChatService : INpcChatService
{
    private readonly Dictionary<(EntityId, int), List<PatternEntry>> _sets = new();
    private readonly Dictionary<EntityId, HashSet<int>> _activeSets = new();
    private readonly ILogger<NpcChatService> _logger;

    public NpcChatService(ILogger<NpcChatService> logger) => _logger = logger;

    public bool DefPattern(NpcEntity npc, int setId, string pattern, string eventName)
    {
        try
        {
            var rx = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (!_sets.TryGetValue((npc.Id, setId), out var list)) _sets[(npc.Id, setId)] = list = new();
            list.Add(new PatternEntry(rx, eventName));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DefPattern: invalid regex {Pattern}", pattern);
            return false;
        }
    }

    public bool ActivatePset(NpcEntity npc, int setId)
    {
        if (!_activeSets.TryGetValue(npc.Id, out var a)) _activeSets[npc.Id] = a = new();
        return a.Add(setId);
    }

    public bool DeactivatePset(NpcEntity npc, int setId)
        => _activeSets.TryGetValue(npc.Id, out var a) && a.Remove(setId);

    public bool DeletePset(NpcEntity npc, int setId)
    {
        DeactivatePset(npc, setId);
        return _sets.Remove((npc.Id, setId));
    }

    public int CheckChat(NpcEntity npc, PlayerEntity speaker, string text)
    {
        if (!_activeSets.TryGetValue(npc.Id, out var active)) return 0;
        var matches = 0;
        foreach (var setId in active)
        {
            if (!_sets.TryGetValue((npc.Id, setId), out var patterns)) continue;
            foreach (var p in patterns)
            {
                if (p.Regex.IsMatch(text))
                {
                    matches++;
                    // The actual event-fire dispatches via the script
                    // engine; entry point reserved here.
                }
            }
        }
        return matches;
    }

    public void DefaultPattern(NpcEntity npc) { }
    public void Finalize(NpcEntity npc)
    {
        foreach (var k in _sets.Keys.Where(k => k.Item1 == npc.Id).ToArray())
            _sets.Remove(k);
        _activeSets.Remove(npc.Id);
    }
    public void FinalizeEntry(NpcEntity npc, int setId, string pattern) { }

    private sealed record PatternEntry(Regex Regex, string EventName);
}
