using Core.Database.Entities;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Instance;
using Map.Server.Movement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NpcHooks = Map.Server.Scripting.Records.NpcHooks;

namespace Map.Server.Tests.Instance;

/// <summary>
/// FEATURE-14 — instance lifecycle: keep/idle timers driven by Tick, owner resolution, party/guild
/// scoping on enter, and a real Destroy that evicts occupants + despawns NPCs.
/// </summary>
public class InstanceServiceTests
{
    private const uint DbId = 100;
    private const int KeepSecs = 3600;  // hard lifetime
    private const int IdleSecs = 300;   // empty timeout

    private long _clock;

    private (InstanceService Svc, FakeEntities Reg, FakeDeath Death, FakeSetpos Setpos) Build(
        int keep = KeepSecs, int idle = IdleSecs)
    {
        _clock = 1_000_000;
        var reg = new FakeEntities();
        var death = new FakeDeath();
        var setpos = new FakeSetpos();
        var svc = new InstanceService(new NoScopes(), setpos, NullLogger<InstanceService>.Instance, reg, death, () => _clock);
        svc.SeedCatalogForTest(new InstanceDbEntity
        {
            InstanceId = DbId, Name = "Test", EnterMap = "1@prt_maze", EnterX = 50, EnterY = 50,
            TimeLimit = keep, IdleTimeout = idle,
        });
        return (svc, reg, death, setpos);
    }

    [Fact]
    public void Created_instance_with_no_users_idles_out()
    {
        var (svc, _, _, _) = Build();
        var id = svc.Create((int)DbId, ownerId: 1, mode: 1 /*IM_CHAR*/);

        _clock += (long)IdleSecs * 1000 - 1;
        svc.Tick(_clock);
        Assert.True(svc.ReqInfo(null!, id)); // not yet — 1ms early

        _clock += 2;
        svc.Tick(_clock);
        Assert.False(svc.ReqInfo(null!, id)); // idle timeout → destroyed
    }

    [Fact]
    public void Enter_rejects_a_non_member_and_accepts_an_owner_party_member()
    {
        var (svc, reg, _, _) = Build();
        var id = svc.Create((int)DbId, ownerId: 5, mode: 2 /*IM_PARTY*/);

        var stranger = NewPc(10, partyId: 9);
        reg.Add(stranger);
        Assert.False(svc.Enter(stranger, id));     // not in owner party 5
        Assert.Equal(0, svc.UsersOf(id));

        var member = NewPc(11, partyId: 5);
        reg.Add(member);
        Assert.True(svc.Enter(member, id));         // party member
        Assert.Equal(1, svc.UsersOf(id));
    }

    [Fact]
    public void Entering_stops_idle_and_arms_keep_so_it_survives_idle_window()
    {
        var (svc, reg, _, _) = Build();
        var id = svc.Create((int)DbId, ownerId: 7, mode: 1);
        var pc = NewPc(7);
        reg.Add(pc);
        Assert.True(svc.Enter(pc, id));

        // Past the idle window — but a player is inside, so the idle timer was stopped.
        _clock += (long)IdleSecs * 1000 + 5000;
        svc.Tick(_clock);
        Assert.True(svc.ReqInfo(null!, id)); // still alive (occupied)
    }

    [Fact]
    public void Last_user_leaving_restarts_the_idle_timer()
    {
        var (svc, reg, _, _) = Build();
        var id = svc.Create((int)DbId, ownerId: 7, mode: 1);
        var pc = NewPc(7);
        reg.Add(pc);
        svc.Enter(pc, id);

        svc.DelUsers(id, 1); // occupancy → 0, idle timer re-armed
        _clock += (long)IdleSecs * 1000 + 1;
        svc.Tick(_clock);
        Assert.False(svc.ReqInfo(null!, id)); // idled out after everyone left
    }

    [Fact]
    public void Keep_timer_destroys_even_with_occupants()
    {
        var (svc, reg, _, _) = Build(keep: 60, idle: 300);
        var id = svc.Create((int)DbId, ownerId: 7, mode: 1);
        var pc = NewPc(7);
        reg.Add(pc);
        svc.Enter(pc, id); // occupied — idle never fires

        _clock += 60L * 1000 + 1; // past keep_limit
        svc.Tick(_clock);
        Assert.False(svc.ReqInfo(null!, id)); // hard lifetime cap → destroyed despite occupant
    }

    [Fact]
    public void Destroy_evicts_occupants_and_despawns_npcs()
    {
        var (svc, reg, death, _) = Build();
        var id = svc.Create((int)DbId, ownerId: 7, mode: 1);
        var pc = NewPc(7);
        reg.Add(pc);
        svc.Enter(pc, id);

        var npc = new NpcEntity(new EntityId(900_001), "Guardian", 100, 1, 50, 50, 0, null, NpcHooks.Empty);
        svc.AddNpc(id, npc);
        Assert.True(reg.Contains(npc.Id)); // registered

        Assert.True(svc.Destroy(id));
        Assert.Contains(pc.CharacterId, death.Warped);   // occupant warped to savepoint
        Assert.False(reg.Contains(npc.Id));              // npc despawned
        Assert.False(svc.ReqInfo(null!, id));            // record gone
    }

    [Fact]
    public void GetOwner_resolves_char_and_party_owners()
    {
        var (svc, reg, _, _) = Build();
        var charOwner = NewPc(42);
        reg.Add(charOwner);
        var idChar = svc.Create((int)DbId, ownerId: 42, mode: 1 /*IM_CHAR*/);
        Assert.Same(charOwner, svc.GetOwner(idChar));

        var partyMember = NewPc(50, partyId: 8);
        reg.Add(partyMember);
        var idParty = svc.Create((int)DbId, ownerId: 8, mode: 2 /*IM_PARTY*/);
        Assert.Same(partyMember, svc.GetOwner(idParty));
    }

    // --- helpers / fakes ---

    private static PlayerEntity NewPc(int charId, int partyId = 0, int guildId = 0)
        => new(charId, charId, $"Pc{charId}", Guid.NewGuid(), 1, 50, 50)
        { Hp = 1, MaxHp = 1, PartyId = partyId, GuildId = guildId };

    private sealed class FakeSetpos : IPcSetposService
    {
        public SetposResult Setpos(PlayerEntity pc, string mapName, short x, short y)
        {
            pc.X = x; pc.Y = y;
            return SetposResult.Ok;
        }
    }

    private sealed class FakeDeath : IPcDeathService
    {
        public readonly List<int> Warped = new();
        public void OnPcDead(PlayerEntity pc, Entity? source) { }
        public void Respawn(PlayerEntity pc) { }
        public bool IsDead(PlayerEntity pc) => false;
        public void SetSavepoint(int characterId, string mapName, short x, short y) { }
        public bool WarpToSavepoint(PlayerEntity pc) { Warped.Add(pc.CharacterId); return true; }
    }

    private sealed class FakeEntities : IEntityRegistry
    {
        private readonly Dictionary<int, Entity> _e = new();
        public void Add(Entity entity) => _e[entity.Id.Value] = entity;
        public Entity? Remove(EntityId id) { _e.Remove(id.Value, out var e); return e; }
        public Entity? Get(EntityId id) => _e.GetValueOrDefault(id.Value);
        public bool Contains(EntityId id) => _e.ContainsKey(id.Value);
        public void Move(EntityId id, short newX, short newY) { }
        public IReadOnlyList<Entity> ForEachInRange(uint mapId, short cx, short cy, short range, EntityType mask) => Array.Empty<Entity>();
        public IReadOnlyList<Entity> ForEachInArea(uint mapId, short x0, short y0, short x1, short y1, EntityType mask) => Array.Empty<Entity>();
        public IEnumerable<Entity> All() => _e.Values;
        public int Count => _e.Count;
    }

    private sealed class NoScopes : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new Scope();
        private sealed class Scope : IServiceScope
        {
            public IServiceProvider ServiceProvider { get; } = new EmptyProvider();
            public void Dispose() { }
        }
        private sealed class EmptyProvider : IServiceProvider
        {
            public object? GetService(Type serviceType) => null; // GetRequiredService throws → LoadCatalog catches
        }
    }
}
