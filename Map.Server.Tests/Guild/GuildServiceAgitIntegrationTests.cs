using Map.Server.Agit;
using Map.Server.Entities;
using Map.Server.Guild;
using Map.Server.Spawn.NpcOps;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Guild;

/// <summary>
/// WOE-1 integration — verifies that when an <see cref="IAgitService"/>
/// is injected into <see cref="GuildService"/>, the
/// <see cref="GuildService.IsAgitActive"/> gate (used by ReqAlliance /
/// DelAlliance / Opposition) follows live WoE state instead of the
/// in-test override flag.
/// </summary>
public class GuildServiceAgitIntegrationTests
{
    [Fact]
    public void IsAgitActive_DelegatesToInjectedAgitService()
    {
        var agit = new AgitService(NullLogger<AgitService>.Instance, npc: null);
        var svc = new GuildService(NullLogger<GuildService>.Instance, agit);

        Assert.False(svc.IsAgitActive);

        agit.AgitStart();
        Assert.True(svc.IsAgitActive);

        agit.AgitEnd();
        Assert.False(svc.IsAgitActive);

        // WoE 2.0 / TE also count
        agit.Agit2Start();
        Assert.True(svc.IsAgitActive);
        agit.Agit2End();
        agit.Agit3Start();
        Assert.True(svc.IsAgitActive);
        agit.Agit3End();
        Assert.False(svc.IsAgitActive);
    }

    [Fact]
    public void IsAgitActive_NoAgitInjected_FollowsOverrideFlag()
    {
        var svc = new GuildService(NullLogger<GuildService>.Instance);
        Assert.False(svc.IsAgitActive);
        svc.IsAgitActive = true;
        Assert.True(svc.IsAgitActive);
        svc.IsAgitActive = false;
        Assert.False(svc.IsAgitActive);
    }

    [Fact]
    public void ReqAlliance_BlocksWhileWoEActive_FromAgitService()
    {
        // Wire a real AgitService into GuildService; flipping the
        // WoE flag should block ReqAlliance as if rAthena's
        // is_agit_start() returned true.
        var agit = new AgitService(NullLogger<AgitService>.Instance, npc: null);
        var svc = new GuildService(NullLogger<GuildService>.Instance, agit);

        // Seed two guilds + masters
        svc.OnRecvInfo(new Core.Server.IPC.GuildInfoData { GuildId = 1, Name = "A", MaxMember = 16, MasterCharacterId = 100 });
        svc.OnRecvInfo(new Core.Server.IPC.GuildInfoData { GuildId = 2, Name = "B", MaxMember = 16, MasterCharacterId = 200 });
        var alpha = new PlayerEntity(100, 1000, "AM", System.Guid.NewGuid(), 1, 100, 100);
        alpha.GuildId = 1;
        var beta = new PlayerEntity(200, 2000, "BM", System.Guid.NewGuid(), 1, 100, 100);
        beta.GuildId = 2;

        // Peacetime: alliance proposal accepted.
        Assert.True(svc.ReqAlliance(alpha, beta));

        // WoE 1.0 fires: proposal blocked (cpp:1856).
        agit.AgitStart();
        Assert.False(svc.ReqAlliance(alpha, beta));
    }
}
