using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Spawn.NpcOps;

/// <summary>Default <see cref="INpcOpsService"/>. Event dispatch + click delegate to ScriptHost when wired.</summary>
public sealed class NpcOpsService : INpcOpsService
{
    private readonly ILogger<NpcOpsService> _logger;
    public NpcOpsService(ILogger<NpcOpsService> logger) => _logger = logger;

    public int Event(PlayerEntity pc, string eventName, byte ontouch) => 0;
    public int EventDo(string eventName) => 0;
    public int EventDoAll(string eventName) => 0;
    public int TimerEventStart(NpcEntity npc, int rid) => 0;
    public int TimerEventStop(NpcEntity npc) => 0;
    public int SetTimerEventTick(NpcEntity npc, long newtick) => 0;
    public long GetTimerEventTick(NpcEntity npc) => 0;
    public int Enable(string npcName, byte flag) => 0;
    public int UnloadFile(string filename) => 0;
    public void Reload() { }
    public int Click(PlayerEntity pc, NpcEntity npc) => 0;
    public int BuyList(PlayerEntity pc, NpcEntity npc, IReadOnlyList<(short index, short qty)> items) => 0;
    public int SellList(PlayerEntity pc, NpcEntity npc, IReadOnlyList<(short index, short qty)> items) => 0;
    public int BuySellSel(PlayerEntity pc, NpcEntity npc, byte type) => 0;
    public int ScriptCont(PlayerEntity pc, NpcEntity npc, byte type) => 0;
    public void GlobalMessage(NpcEntity npc, string text) { }
    public int SetDisplayName(NpcEntity npc, string name) => 0;
    public int SetClass(NpcEntity npc, short newClass) => 0;
}
