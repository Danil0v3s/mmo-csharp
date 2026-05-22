using Map.Server.Spawn.NpcOps;
using Microsoft.Extensions.Logging;

namespace Map.Server.Agit;

/// <summary>
/// Default <see cref="IAgitService"/>. Three independent boolean
/// flags (WoE 1.0 / 2.0 / TE) that fire <c>OnAgitStart*</c> /
/// <c>OnAgitEnd*</c> NPC events on transition.
///
/// State lives in plain bool fields because the game loop is
/// single-threaded; concurrent reads from the network thread are
/// safe (single-byte writes) and the event dispatch is queued to
/// the loop via the NPC service.
/// </summary>
public sealed class AgitService : IAgitService
{
    private readonly ILogger<AgitService> _logger;
    private readonly INpcOpsService? _npc;

    private bool _agit;
    private bool _agit2;
    private bool _agit3;

    public AgitService(ILogger<AgitService> logger, INpcOpsService? npc = null)
    {
        _logger = logger;
        _npc = npc;
    }

    public bool IsAgitActive => _agit;
    public bool IsAgit2Active => _agit2;
    public bool IsAgit3Active => _agit3;
    public bool IsAnyActive => _agit || _agit2 || _agit3;

    public bool AgitStart()
    {
        if (_agit) return false;
        _agit = true;
        _logger.LogInformation("WoE 1.0 started");
        Fire(AgitEventNames.Start);
        return true;
    }

    public bool AgitEnd()
    {
        if (!_agit) return false;
        _agit = false;
        _logger.LogInformation("WoE 1.0 ended");
        Fire(AgitEventNames.End);
        return true;
    }

    public bool Agit2Start()
    {
        if (_agit2) return false;
        _agit2 = true;
        _logger.LogInformation("WoE 2.0 started");
        Fire(AgitEventNames.Start2);
        return true;
    }

    public bool Agit2End()
    {
        if (!_agit2) return false;
        _agit2 = false;
        _logger.LogInformation("WoE 2.0 ended");
        Fire(AgitEventNames.End2);
        return true;
    }

    public bool Agit3Start()
    {
        if (_agit3) return false;
        _agit3 = true;
        _logger.LogInformation("WoE TE started");
        Fire(AgitEventNames.Start3);
        return true;
    }

    public bool Agit3End()
    {
        if (!_agit3) return false;
        _agit3 = false;
        _logger.LogInformation("WoE TE ended");
        Fire(AgitEventNames.End3);
        return true;
    }

    public void EndAll()
    {
        AgitEnd();
        Agit2End();
        Agit3End();
    }

    private void Fire(string eventName)
    {
        // INpcOpsService.EventDoAll iterates registered NPCs and
        // dispatches; safe to call when the NPC service is null
        // (boot-time order, tests).
        try
        {
            _npc?.EventDoAll(eventName);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Agit NPC event {Event} dispatch failed", eventName);
        }
    }
}
