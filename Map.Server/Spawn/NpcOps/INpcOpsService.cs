using Map.Server.Entities;

namespace Map.Server.Spawn.NpcOps;

/// <summary>
/// NPC placement + event dispatch. Canonical entry points for
/// rAthena <c>npc.cpp</c> (6 341 lines, 76 public functions).
///
/// The TS-script + Jint engine handles dialog logic; this service
/// owns the spawn / despawn / event-trigger / parse helpers.
/// </summary>
public interface INpcOpsService
{
    /// <summary>rAthena <c>npc_event</c> — trigger an event by name.</summary>
    int Event(PlayerEntity pc, string eventName, byte ontouch);

    /// <summary>rAthena <c>npc_event_do</c>.</summary>
    int EventDo(string eventName);

    /// <summary>rAthena <c>npc_event_doall</c>.</summary>
    int EventDoAll(string eventName);

    /// <summary>rAthena <c>npc_timerevent_start</c>.</summary>
    int TimerEventStart(NpcEntity npc, int rid);

    /// <summary>rAthena <c>npc_timerevent_stop</c>.</summary>
    int TimerEventStop(NpcEntity npc);

    /// <summary>rAthena <c>npc_settimerevent_tick</c>.</summary>
    int SetTimerEventTick(NpcEntity npc, long newtick);

    /// <summary>rAthena <c>npc_gettimerevent_tick</c>.</summary>
    long GetTimerEventTick(NpcEntity npc);

    /// <summary>rAthena <c>npc_enable</c>.</summary>
    int Enable(string npcName, byte flag);

    /// <summary>rAthena <c>npc_unloadfile</c>.</summary>
    int UnloadFile(string filename);

    /// <summary>rAthena <c>npc_reload</c>.</summary>
    void Reload();

    /// <summary>rAthena <c>npc_click</c>.</summary>
    int Click(PlayerEntity pc, NpcEntity npc);

    /// <summary>rAthena <c>npc_buylist</c>.</summary>
    int BuyList(PlayerEntity pc, NpcEntity npc, IReadOnlyList<(short index, short qty)> items);

    /// <summary>rAthena <c>npc_selllist</c>.</summary>
    int SellList(PlayerEntity pc, NpcEntity npc, IReadOnlyList<(short index, short qty)> items);

    /// <summary>rAthena <c>npc_buysellsel</c>.</summary>
    int BuySellSel(PlayerEntity pc, NpcEntity npc, byte type);

    /// <summary>rAthena <c>npc_scriptcont</c>.</summary>
    int ScriptCont(PlayerEntity pc, NpcEntity npc, byte type);

    /// <summary>rAthena <c>npc_globalmessage</c>.</summary>
    void GlobalMessage(NpcEntity npc, string text);

    /// <summary>rAthena <c>npc_setdisplayname</c>.</summary>
    int SetDisplayName(NpcEntity npc, string name);

    /// <summary>rAthena <c>npc_setclass</c>.</summary>
    int SetClass(NpcEntity npc, short newClass);
}
