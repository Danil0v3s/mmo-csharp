using Map.Server.Services;
using Map.Server.Services.Intif;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Services;

/// <summary>
/// T7.3 — verifies IntifService dispatches the Homun / Merc / Elem
/// (Create / Request / Save / Delete) entry points through their
/// typed ICharServerIpcService* sub-IPCs when wired, and short-
/// circuits to 0 otherwise. 12 dispatch tests (4 per family).
/// </summary>
public class IntifCompanionWiringTests
{
    // ---- Homunculus ----

    [Fact]
    public void HomunculusCreate_WithIpc_Dispatches()
    {
        var fake = new RecordingHomunIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, homunIpc: fake);
        Assert.Equal(1, intif.HomunculusCreate(accountId: 5, data: new byte[] { 1, 2, 3, 4 }));
        Assert.Single(fake.CreateCalls);
        Assert.Equal(5, fake.CreateCalls[0].AccountId);
    }

    [Fact]
    public void HomunculusRequest_WithIpc_Dispatches()
    {
        var fake = new RecordingHomunIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, homunIpc: fake);
        Assert.Equal(1, intif.HomunculusRequest(accountId: 5, homunId: 11));
        Assert.Single(fake.LoadCalls);
        Assert.Equal(11, fake.LoadCalls[0].HomunculusId);
    }

    [Fact]
    public void HomunculusSave_WithIpc_DispatchesLegacyPayloadWhenNoLiveSnapshot()
    {
        var fake = new RecordingHomunIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, homunIpc: fake);
        // Legacy byte[] — first 4 bytes = homun_id (= 7).
        var data = new byte[] { 7, 0, 0, 0, 0xff };
        Assert.Equal(1, intif.HomunculusSave(data));
        Assert.Single(fake.SaveCalls);
        Assert.Equal(7, fake.SaveCalls[0].HomunculusId);
    }

    [Fact]
    public void HomunculusDelete_WithIpc_Dispatches()
    {
        var fake = new RecordingHomunIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, homunIpc: fake);
        Assert.Equal(1, intif.HomunculusDelete(homunId: 99));
        Assert.Single(fake.DeleteCalls);
        Assert.Equal(99, fake.DeleteCalls[0]);
    }

    // ---- Mercenary ----

    [Fact]
    public void MercenaryCreate_WithIpc_Dispatches()
    {
        var fake = new RecordingMercIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, mercIpc: fake);
        Assert.Equal(1, intif.MercenaryCreate(data: new byte[] { 0xa }));
        Assert.Single(fake.CreateCalls);
    }

    [Fact]
    public void MercenaryRequest_WithIpc_Dispatches()
    {
        var fake = new RecordingMercIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, mercIpc: fake);
        Assert.Equal(1, intif.MercenaryRequest(accountId: 3, mercId: 14));
        Assert.Single(fake.LoadCalls);
        Assert.Equal(14, fake.LoadCalls[0]);
    }

    [Fact]
    public void MercenarySave_WithIpc_Dispatches()
    {
        var fake = new RecordingMercIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, mercIpc: fake);
        // merc_id = 17 in first 4 LE bytes
        Assert.Equal(1, intif.MercenarySave(data: new byte[] { 17, 0, 0, 0 }));
        Assert.Single(fake.SaveCalls);
        Assert.Equal(17, fake.SaveCalls[0].MercenaryId);
    }

    [Fact]
    public void MercenaryDelete_WithIpc_Dispatches()
    {
        var fake = new RecordingMercIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, mercIpc: fake);
        Assert.Equal(1, intif.MercenaryDelete(mercId: 5));
        Assert.Single(fake.DeleteCalls);
        Assert.Equal(5, fake.DeleteCalls[0]);
    }

    // ---- Elemental ----

    [Fact]
    public void ElementalCreate_WithIpc_Dispatches()
    {
        var fake = new RecordingElemIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, elemIpc: fake);
        // class_id = 2114 in first 4 LE bytes
        Assert.Equal(1, intif.ElementalCreate(data: new byte[] { 0x42, 0x08, 0, 0 }));
        Assert.Single(fake.CreateCalls);
        Assert.Equal(0x842, fake.CreateCalls[0].ClassId);
    }

    [Fact]
    public void ElementalRequest_WithIpc_Dispatches()
    {
        var fake = new RecordingElemIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, elemIpc: fake);
        Assert.Equal(1, intif.ElementalRequest(accountId: 3, eleId: 21));
        Assert.Single(fake.LoadCalls);
        Assert.Equal(21, fake.LoadCalls[0]);
    }

    [Fact]
    public void ElementalSave_WithIpc_Dispatches()
    {
        var fake = new RecordingElemIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, elemIpc: fake);
        Assert.Equal(1, intif.ElementalSave(data: new byte[] { 6, 0, 0, 0 }));
        Assert.Single(fake.SaveCalls);
        Assert.Equal(6, fake.SaveCalls[0].ElementalId);
    }

    [Fact]
    public void ElementalDelete_WithIpc_Dispatches()
    {
        var fake = new RecordingElemIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, elemIpc: fake);
        Assert.Equal(1, intif.ElementalDelete(eleId: 33));
        Assert.Single(fake.DeleteCalls);
        Assert.Equal(33, fake.DeleteCalls[0]);
    }

    // ---- Fakes ----

    private sealed class RecordingHomunIpc : ICharServerIpcServiceHomunculus
    {
        public sealed record CreateCall(int AccountId, Core.Server.IPC.HomunculusData Data);
        public sealed record LoadCall(int AccountId, int HomunculusId);
        public sealed record SaveCall(int AccountId, int HomunculusId);
        public List<CreateCall> CreateCalls { get; } = new();
        public List<LoadCall> LoadCalls { get; } = new();
        public List<SaveCall> SaveCalls { get; } = new();
        public List<int> DeleteCalls { get; } = new();

        public Task<Core.Server.IPC.HomunculusCreateResponse?> HomunculusCreateAsync(
            int accountId, Core.Server.IPC.HomunculusData homunculus,
            CancellationToken cancellationToken = default)
        {
            CreateCalls.Add(new CreateCall(accountId, homunculus));
            return Task.FromResult<Core.Server.IPC.HomunculusCreateResponse?>(null);
        }
        public Task<Core.Server.IPC.HomunculusLoadResponse?> HomunculusLoadAsync(
            int accountId, int homunculusId, CancellationToken cancellationToken = default)
        {
            LoadCalls.Add(new LoadCall(accountId, homunculusId));
            return Task.FromResult<Core.Server.IPC.HomunculusLoadResponse?>(null);
        }
        public Task<Core.Server.IPC.HomunculusSaveResponse?> HomunculusSaveAsync(
            int accountId, Core.Server.IPC.HomunculusData homunculus,
            CancellationToken cancellationToken = default)
        {
            SaveCalls.Add(new SaveCall(accountId, homunculus.HomunculusId));
            return Task.FromResult<Core.Server.IPC.HomunculusSaveResponse?>(null);
        }
        public Task<Core.Server.IPC.HomunculusDeleteResponse?> HomunculusDeleteAsync(
            int homunculusId, CancellationToken cancellationToken = default)
        {
            DeleteCalls.Add(homunculusId);
            return Task.FromResult<Core.Server.IPC.HomunculusDeleteResponse?>(null);
        }
        public Task<Core.Server.IPC.HomunculusRenameResponse?> HomunculusRenameAsync(
            int accountId, int characterId, string name,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Core.Server.IPC.HomunculusRenameResponse?>(null);
    }

    private sealed class RecordingMercIpc : ICharServerIpcServiceMercenary
    {
        public List<Core.Server.IPC.MercenaryData> CreateCalls { get; } = new();
        public List<int> LoadCalls { get; } = new();
        public List<Core.Server.IPC.MercenaryData> SaveCalls { get; } = new();
        public List<int> DeleteCalls { get; } = new();

        public Task<Core.Server.IPC.MercenaryCreateResponse?> MercenaryCreateAsync(
            Core.Server.IPC.MercenaryData mercenary, CancellationToken cancellationToken = default)
        {
            CreateCalls.Add(mercenary);
            return Task.FromResult<Core.Server.IPC.MercenaryCreateResponse?>(null);
        }
        public Task<Core.Server.IPC.MercenaryLoadResponse?> MercenaryLoadAsync(
            int mercenaryId, int characterId, CancellationToken cancellationToken = default)
        {
            LoadCalls.Add(mercenaryId);
            return Task.FromResult<Core.Server.IPC.MercenaryLoadResponse?>(null);
        }
        public Task<Core.Server.IPC.MercenarySaveResponse?> MercenarySaveAsync(
            Core.Server.IPC.MercenaryData mercenary, CancellationToken cancellationToken = default)
        {
            SaveCalls.Add(mercenary);
            return Task.FromResult<Core.Server.IPC.MercenarySaveResponse?>(null);
        }
        public Task<Core.Server.IPC.MercenaryDeleteResponse?> MercenaryDeleteAsync(
            int mercenaryId, CancellationToken cancellationToken = default)
        {
            DeleteCalls.Add(mercenaryId);
            return Task.FromResult<Core.Server.IPC.MercenaryDeleteResponse?>(null);
        }
    }

    private sealed class RecordingElemIpc : ICharServerIpcServiceElemental
    {
        public List<Core.Server.IPC.ElementalData> CreateCalls { get; } = new();
        public List<int> LoadCalls { get; } = new();
        public List<Core.Server.IPC.ElementalData> SaveCalls { get; } = new();
        public List<int> DeleteCalls { get; } = new();

        public Task<Core.Server.IPC.ElementalCreateResponse?> ElementalCreateAsync(
            Core.Server.IPC.ElementalData elemental, CancellationToken cancellationToken = default)
        {
            CreateCalls.Add(elemental);
            return Task.FromResult<Core.Server.IPC.ElementalCreateResponse?>(null);
        }
        public Task<Core.Server.IPC.ElementalLoadResponse?> ElementalLoadAsync(
            int elementalId, int characterId, CancellationToken cancellationToken = default)
        {
            LoadCalls.Add(elementalId);
            return Task.FromResult<Core.Server.IPC.ElementalLoadResponse?>(null);
        }
        public Task<Core.Server.IPC.ElementalSaveResponse?> ElementalSaveAsync(
            Core.Server.IPC.ElementalData elemental, CancellationToken cancellationToken = default)
        {
            SaveCalls.Add(elemental);
            return Task.FromResult<Core.Server.IPC.ElementalSaveResponse?>(null);
        }
        public Task<Core.Server.IPC.ElementalDeleteResponse?> ElementalDeleteAsync(
            int elementalId, CancellationToken cancellationToken = default)
        {
            DeleteCalls.Add(elementalId);
            return Task.FromResult<Core.Server.IPC.ElementalDeleteResponse?>(null);
        }
    }
}
