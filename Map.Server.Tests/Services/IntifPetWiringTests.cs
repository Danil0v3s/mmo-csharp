using Map.Server.Entities;
using Map.Server.Pet;
using Map.Server.Services;
using Map.Server.Services.Intif;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Services;

/// <summary>
/// T7.2 — verifies IntifService.PetCreate / RequestPetInfo / SavePet /
/// DeletePet dispatch through ICharServerIpcServicePet when wired, and
/// short-circuit to 0 otherwise. SavePet also exercises the
/// PetService.SerializeSnapshot path — SavePet without a live pet
/// returns 0 (the rAthena "no in-memory pet" branch).
/// </summary>
public class IntifPetWiringTests
{
    [Fact]
    public void PetCreate_WithIpc_DispatchesCreateAsync()
    {
        var fake = new RecordingPetIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, petIpc: fake);
        var master = MakePc(charId: 1, accountId: 2);

        var rc = intif.PetCreate(master, classId: 1002, nameId: 0, rename: 0,
            eggItemId: 9001, intimate: 250, hungry: 80, gender: 'M', petName: "Poring");

        Assert.Equal(1, rc);
        Assert.Single(fake.CreateCalls);
        Assert.Equal(2, fake.CreateCalls[0].AccountId);
        Assert.Equal(1, fake.CreateCalls[0].CharacterId);
        Assert.Equal(1002, fake.CreateCalls[0].ClassId);
        Assert.Equal(9001, fake.CreateCalls[0].EggItemId);
        Assert.Equal("Poring", fake.CreateCalls[0].Name);
    }

    [Fact]
    public void RequestPetInfo_WithIpc_DispatchesLoadAsync()
    {
        var fake = new RecordingPetIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, petIpc: fake);

        Assert.Equal(1, intif.RequestPetInfo(petId: 17, accountId: 5, flag: 0));
        Assert.Single(fake.LoadCalls);
        Assert.Equal(17, fake.LoadCalls[0].PetId);
        Assert.Equal(5, fake.LoadCalls[0].AccountId);
    }

    [Fact]
    public void SavePet_WithoutLivePet_ReturnsZeroAndSkipsDispatch()
    {
        var fake = new RecordingPetIpc();
        var petService = new RecordingPetService(snapshotFor: null);
        var intif = new IntifService(NullLogger<IntifService>.Instance,
            petIpc: fake, petService: petService);

        // No live pet for id 99 → snapshot returns null, dispatch skipped.
        Assert.Equal(0, intif.SavePet(petId: 99));
        Assert.Empty(fake.SaveCalls);
    }

    [Fact]
    public void SavePet_WithLivePet_DispatchesSaveAsync()
    {
        var fake = new RecordingPetIpc();
        var snapshot = new Core.Server.IPC.PetData
        {
            PetId = 42,
            AccountId = 7,
            CharacterId = 1,
            ClassId = 1002,
            Intimacy = 700,
            Hungry = 50,
            Name = "Pori",
        };
        var petService = new RecordingPetService(snapshotFor: snapshot);
        var intif = new IntifService(NullLogger<IntifService>.Instance,
            petIpc: fake, petService: petService);

        Assert.Equal(1, intif.SavePet(petId: 42));
        Assert.Single(fake.SaveCalls);
        Assert.Equal(7, fake.SaveCalls[0].AccountId);
        Assert.Equal(42, fake.SaveCalls[0].Pet.PetId);
        Assert.Equal(700, fake.SaveCalls[0].Pet.Intimacy);
    }

    [Fact]
    public void DeletePet_WithIpc_DispatchesDeleteAsync()
    {
        var fake = new RecordingPetIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, petIpc: fake);

        Assert.Equal(1, intif.DeletePet(petId: 88));
        Assert.Single(fake.DeleteCalls);
        Assert.Equal(88, fake.DeleteCalls[0]);
    }

    private static PlayerEntity MakePc(int charId, int accountId) =>
        new(charId, accountId, $"P{charId}", Guid.NewGuid(), mapId: 1, x: 0, y: 0);

    /// <summary>
    /// Minimal IPetService for the dispatch tests — only
    /// <see cref="SerializeSnapshot"/> is exercised; the lifecycle
    /// methods throw so a regression won't pass them silently.
    /// </summary>
    private sealed class RecordingPetService : IPetService
    {
        private readonly Core.Server.IPC.PetData? _snapshot;
        public RecordingPetService(Core.Server.IPC.PetData? snapshotFor) => _snapshot = snapshotFor;
        public PetEntity? Summon(PlayerEntity owner, int petClassId, string petName, int eggItemId = 0, long petId = 0, int intimacy = -1, int hunger = -1, bool renamed = false) => throw new NotImplementedException();
        public void Recall(PlayerEntity owner) => throw new NotImplementedException();
        public void Tick(long nowTick) { }
        public Core.Server.IPC.PetData? SerializeSnapshot(int petId) => _snapshot;
        public bool TryGetLivePetId(PlayerEntity owner, out int petId) { petId = 0; return false; }
    }

    private sealed class RecordingPetIpc : ICharServerIpcServicePet
    {
        public sealed record CreateCall(int AccountId, int CharacterId, int ClassId,
            int Level, int EggItemId, int EquipItemId, int Intimacy, int Hungry,
            int RenameFlag, bool Incubate, string Name);
        public sealed record LoadCall(int AccountId, int CharacterId, int PetId);
        public sealed record SaveCall(int AccountId, Core.Server.IPC.PetData Pet);

        public List<CreateCall> CreateCalls { get; } = new();
        public List<LoadCall> LoadCalls { get; } = new();
        public List<SaveCall> SaveCalls { get; } = new();
        public List<int> DeleteCalls { get; } = new();

        public Task<Core.Server.IPC.PetCreateResponse?> PetCreateAsync(
            int accountId, int characterId, int classId, int level,
            int eggItemId, int equipItemId, int intimacy, int hungry,
            int renameFlag, bool incubate, string name,
            CancellationToken cancellationToken = default)
        {
            CreateCalls.Add(new CreateCall(accountId, characterId, classId, level,
                eggItemId, equipItemId, intimacy, hungry, renameFlag, incubate, name));
            return Task.FromResult<Core.Server.IPC.PetCreateResponse?>(null);
        }

        public Task<Core.Server.IPC.PetLoadResponse?> PetLoadAsync(
            int accountId, int characterId, int petId,
            CancellationToken cancellationToken = default)
        {
            LoadCalls.Add(new LoadCall(accountId, characterId, petId));
            return Task.FromResult<Core.Server.IPC.PetLoadResponse?>(null);
        }

        public Task<Core.Server.IPC.PetSaveResponse?> PetSaveAsync(
            int accountId, Core.Server.IPC.PetData pet,
            CancellationToken cancellationToken = default)
        {
            SaveCalls.Add(new SaveCall(accountId, pet));
            return Task.FromResult<Core.Server.IPC.PetSaveResponse?>(null);
        }

        public Task<Core.Server.IPC.PetDeleteResponse?> PetDeleteAsync(
            int petId, CancellationToken cancellationToken = default)
        {
            DeleteCalls.Add(petId);
            return Task.FromResult<Core.Server.IPC.PetDeleteResponse?>(null);
        }
    }
}
