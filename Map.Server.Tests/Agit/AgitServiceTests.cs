using Map.Server.Agit;
using Map.Server.Entities;
using Map.Server.Spawn.NpcOps;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Agit;

/// <summary>
/// WOE-1 — verifies the WoE state machine (rAthena
/// <c>guild_agit_start</c> / <c>_end</c> / <c>guild_agit2_*</c> /
/// <c>guild_agit3_*</c> at guild.cpp:2532+). Each Start/End is
/// idempotent and fires the matching OnAgitStart* / OnAgitEnd* NPC
/// event via <see cref="INpcOpsService.EventDoAll"/>.
/// </summary>
public class AgitServiceTests
{
    [Fact]
    public void Defaults_AllFlagsFalse()
    {
        var svc = Build(out _);
        Assert.False(svc.IsAgitActive);
        Assert.False(svc.IsAgit2Active);
        Assert.False(svc.IsAgit3Active);
        Assert.False(svc.IsAnyActive);
    }

    [Fact]
    public void AgitStart_FiresStartEvent_AndFlipsFlag()
    {
        var svc = Build(out var npc);
        Assert.True(svc.AgitStart());
        Assert.True(svc.IsAgitActive);
        Assert.True(svc.IsAnyActive);
        Assert.Equal(1, npc.EventsFired(AgitEventNames.Start));
    }

    [Fact]
    public void AgitStart_Idempotent()
    {
        var svc = Build(out var npc);
        Assert.True(svc.AgitStart());
        // second start is a no-op — no NPC event fired
        Assert.False(svc.AgitStart());
        Assert.Equal(1, npc.EventsFired(AgitEventNames.Start));
    }

    [Fact]
    public void AgitEnd_FiresEndEvent_AndClearsFlag()
    {
        var svc = Build(out var npc);
        svc.AgitStart();
        Assert.True(svc.AgitEnd());
        Assert.False(svc.IsAgitActive);
        Assert.False(svc.IsAnyActive);
        Assert.Equal(1, npc.EventsFired(AgitEventNames.End));
    }

    [Fact]
    public void AgitEnd_WithoutStart_ReturnsFalse()
    {
        var svc = Build(out var npc);
        Assert.False(svc.AgitEnd());
        Assert.Equal(0, npc.EventsFired(AgitEventNames.End));
    }

    [Fact]
    public void Agit2_AndAgit3_AreIndependent()
    {
        var svc = Build(out var npc);
        Assert.True(svc.Agit2Start());
        Assert.True(svc.Agit3Start());
        Assert.False(svc.IsAgitActive);   // WoE 1.0 still off
        Assert.True(svc.IsAgit2Active);
        Assert.True(svc.IsAgit3Active);
        Assert.True(svc.IsAnyActive);
        Assert.Equal(1, npc.EventsFired(AgitEventNames.Start2));
        Assert.Equal(1, npc.EventsFired(AgitEventNames.Start3));
    }

    [Fact]
    public void Agit2_Idempotent()
    {
        var svc = Build(out var npc);
        Assert.True(svc.Agit2Start());
        Assert.False(svc.Agit2Start());
        Assert.True(svc.Agit2End());
        Assert.False(svc.Agit2End());
        Assert.Equal(1, npc.EventsFired(AgitEventNames.Start2));
        Assert.Equal(1, npc.EventsFired(AgitEventNames.End2));
    }

    [Fact]
    public void Agit3_Idempotent()
    {
        var svc = Build(out var npc);
        Assert.True(svc.Agit3Start());
        Assert.False(svc.Agit3Start());
        Assert.True(svc.Agit3End());
        Assert.False(svc.Agit3End());
        Assert.Equal(1, npc.EventsFired(AgitEventNames.Start3));
        Assert.Equal(1, npc.EventsFired(AgitEventNames.End3));
    }

    [Fact]
    public void EndAll_StopsEveryEdition()
    {
        var svc = Build(out var npc);
        svc.AgitStart();
        svc.Agit2Start();
        svc.Agit3Start();
        Assert.True(svc.IsAnyActive);

        svc.EndAll();

        Assert.False(svc.IsAnyActive);
        Assert.False(svc.IsAgitActive);
        Assert.False(svc.IsAgit2Active);
        Assert.False(svc.IsAgit3Active);
        Assert.Equal(1, npc.EventsFired(AgitEventNames.End));
        Assert.Equal(1, npc.EventsFired(AgitEventNames.End2));
        Assert.Equal(1, npc.EventsFired(AgitEventNames.End3));
    }

    [Fact]
    public void EndAll_NoActiveWoE_NoEvents()
    {
        var svc = Build(out var npc);
        svc.EndAll();
        Assert.Equal(0, npc.EventsFired(AgitEventNames.End));
        Assert.Equal(0, npc.EventsFired(AgitEventNames.End2));
        Assert.Equal(0, npc.EventsFired(AgitEventNames.End3));
    }

    [Fact]
    public void NoNpcServiceInjected_StillFlipsState()
    {
        // Boot-time / test path where INpcOpsService isn't wired.
        // State must still flip so the alliance gates work.
        var svc = new AgitService(NullLogger<AgitService>.Instance, npc: null);
        Assert.True(svc.AgitStart());
        Assert.True(svc.IsAgitActive);
        Assert.True(svc.AgitEnd());
        Assert.False(svc.IsAgitActive);
    }

    [Fact]
    public void NpcEventDispatchThrows_DoesNotCrashCaller()
    {
        var npc = new ThrowingNpc();
        var svc = new AgitService(NullLogger<AgitService>.Instance, npc);
        // Must not throw — error logged + state still flips
        Assert.True(svc.AgitStart());
        Assert.True(svc.IsAgitActive);
    }

    // -----------------------------------------------------------------

    private static AgitService Build(out FakeNpc npc)
    {
        npc = new FakeNpc();
        return new AgitService(NullLogger<AgitService>.Instance, npc);
    }

    private sealed class FakeNpc : INpcOpsService
    {
        private readonly System.Collections.Generic.Dictionary<string, int> _eventCounts = new();
        public int EventsFired(string name) => _eventCounts.TryGetValue(name, out var n) ? n : 0;

        public int EventDoAll(string eventName)
        {
            _eventCounts[eventName] = EventsFired(eventName) + 1;
            return 1;
        }

        // Unused interface members — throw to catch accidental use.
        public int Event(PlayerEntity pc, string eventName, byte ontouch) => throw new System.NotImplementedException();
        public int EventDo(string eventName) => throw new System.NotImplementedException();
        public int TimerEventStart(NpcEntity npc, int rid) => throw new System.NotImplementedException();
        public int TimerEventStop(NpcEntity npc) => throw new System.NotImplementedException();
        public int SetTimerEventTick(NpcEntity npc, long newtick) => throw new System.NotImplementedException();
        public long GetTimerEventTick(NpcEntity npc) => throw new System.NotImplementedException();
        public int Enable(string npcName, byte flag) => throw new System.NotImplementedException();
        public int UnloadFile(string filename) => throw new System.NotImplementedException();
        public void Reload() => throw new System.NotImplementedException();
        public int Click(PlayerEntity pc, NpcEntity npc) => throw new System.NotImplementedException();
        public int BuyList(PlayerEntity pc, NpcEntity npc, System.Collections.Generic.IReadOnlyList<(short index, short qty)> items) => throw new System.NotImplementedException();
        public int SellList(PlayerEntity pc, NpcEntity npc, System.Collections.Generic.IReadOnlyList<(short index, short qty)> items) => throw new System.NotImplementedException();
        public int BuySellSel(PlayerEntity pc, NpcEntity npc, byte type) => throw new System.NotImplementedException();
        public int ScriptCont(PlayerEntity pc, NpcEntity npc, byte type) => throw new System.NotImplementedException();
        public void GlobalMessage(NpcEntity npc, string text) => throw new System.NotImplementedException();
        public int SetDisplayName(NpcEntity npc, string name) => throw new System.NotImplementedException();
        public int SetClass(NpcEntity npc, short newClass) => throw new System.NotImplementedException();
    }

    private sealed class ThrowingNpc : INpcOpsService
    {
        public int EventDoAll(string eventName) => throw new System.InvalidOperationException("script engine boom");

        public int Event(PlayerEntity pc, string eventName, byte ontouch) => throw new System.NotImplementedException();
        public int EventDo(string eventName) => throw new System.NotImplementedException();
        public int TimerEventStart(NpcEntity npc, int rid) => throw new System.NotImplementedException();
        public int TimerEventStop(NpcEntity npc) => throw new System.NotImplementedException();
        public int SetTimerEventTick(NpcEntity npc, long newtick) => throw new System.NotImplementedException();
        public long GetTimerEventTick(NpcEntity npc) => throw new System.NotImplementedException();
        public int Enable(string npcName, byte flag) => throw new System.NotImplementedException();
        public int UnloadFile(string filename) => throw new System.NotImplementedException();
        public void Reload() => throw new System.NotImplementedException();
        public int Click(PlayerEntity pc, NpcEntity npc) => throw new System.NotImplementedException();
        public int BuyList(PlayerEntity pc, NpcEntity npc, System.Collections.Generic.IReadOnlyList<(short index, short qty)> items) => throw new System.NotImplementedException();
        public int SellList(PlayerEntity pc, NpcEntity npc, System.Collections.Generic.IReadOnlyList<(short index, short qty)> items) => throw new System.NotImplementedException();
        public int BuySellSel(PlayerEntity pc, NpcEntity npc, byte type) => throw new System.NotImplementedException();
        public int ScriptCont(PlayerEntity pc, NpcEntity npc, byte type) => throw new System.NotImplementedException();
        public void GlobalMessage(NpcEntity npc, string text) => throw new System.NotImplementedException();
        public int SetDisplayName(NpcEntity npc, string name) => throw new System.NotImplementedException();
        public int SetClass(NpcEntity npc, short newClass) => throw new System.NotImplementedException();
    }
}
