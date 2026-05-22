using Map.Server.Services;
using Map.Server.Services.Intif;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Services;

/// <summary>
/// T7.8 — verifies IntifService.RequestMapreg / SaveMapreg dispatch
/// through ICharServerIpcServiceMapreg when wired, and short-circuit
/// to 0 otherwise. Closes the last ⚠️ in intif-parity.md (mapreg pair).
/// The char-side gRPC binding is deferred to the script engine's
/// $var consumer port; this test pins the canonical-seam contract.
/// </summary>
public class IntifMapregWiringTests
{
    [Fact]
    public void RequestMapreg_WithIpc_Dispatches()
    {
        var fake = new RecordingMapregIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, mapregIpc: fake);
        Assert.Equal(1, intif.RequestMapreg());
        Assert.Equal(1, fake.RequestCalls);
    }

    [Fact]
    public void RequestMapreg_WithoutIpc_ReturnsZero()
    {
        var intif = new IntifService(NullLogger<IntifService>.Instance);
        Assert.Equal(0, intif.RequestMapreg());
    }

    [Fact]
    public void SaveMapreg_WithIpc_Dispatches()
    {
        var fake = new RecordingMapregIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, mapregIpc: fake);
        var data = new byte[] { 0xde, 0xad };
        Assert.Equal(1, intif.SaveMapreg(data));
        Assert.Single(fake.SaveCalls);
        Assert.Equal(data, fake.SaveCalls[0]);
    }

    [Fact]
    public void SaveMapreg_WithoutIpc_ReturnsZero()
    {
        var intif = new IntifService(NullLogger<IntifService>.Instance);
        Assert.Equal(0, intif.SaveMapreg(new byte[] { 0x1 }));
    }

    private sealed class RecordingMapregIpc : ICharServerIpcServiceMapreg
    {
        public int RequestCalls { get; private set; }
        public List<byte[]> SaveCalls { get; } = new();

        public Task<bool> RequestMapregAsync(CancellationToken cancellationToken = default)
        {
            RequestCalls++;
            return Task.FromResult(true);
        }

        public Task<bool> SaveMapregAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            SaveCalls.Add(data);
            return Task.FromResult(true);
        }
    }
}
