using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Spawn;
using Map.Server.Visibility;
using Microsoft.Extensions.Logging;

namespace Map.Server.Combat;

public sealed class DamageService : IDamageService
{
    private readonly IVisibilityService _visibility;
    private readonly IMobSpawnService _mobSpawn;
    private readonly IEntityRegistry _entities;
    private readonly ILogger<DamageService> _logger;

    public DamageService(
        IVisibilityService visibility,
        IMobSpawnService mobSpawn,
        IEntityRegistry entities,
        ILogger<DamageService> logger)
    {
        _visibility = visibility;
        _mobSpawn = mobSpawn;
        _entities = entities;
        _logger = logger;
    }

    public int ApplyDamage(Entity target, int damage, Entity? source = null)
    {
        if (damage < 0) damage = 0;

        var (currentHp, _) = GetHp(target);
        var actual = Math.Min(damage, currentHp);
        SetHp(target, currentHp - actual);

        BroadcastAct(target, source, actual);

        var remaining = currentHp - actual;
        if (remaining <= 0)
        {
            HandleDeath(target, source);
        }
        return actual;
    }

    private static (int Hp, int MaxHp) GetHp(Entity entity) => entity switch
    {
        MobEntity m => (m.Hp, m.MaxHp),
        PlayerEntity p => (p.Hp, p.MaxHp),
        _ => (1, 1),
    };

    private static void SetHp(Entity entity, int newHp)
    {
        switch (entity)
        {
            case MobEntity m: m.Hp = Math.Max(0, newHp); break;
            case PlayerEntity p: p.Hp = Math.Max(0, newHp); break;
        }
    }

    private void BroadcastAct(Entity target, Entity? source, int damage)
    {
        var packet = new ZC_NOTIFY_ACT3
        {
            SourceId = source?.Id.Value ?? 0,
            TargetId = target.Id.Value,
            ServerTick = (uint)Environment.TickCount,
            // No real motion timing yet; placeholders satisfy the packet
            // shape until ASPD wiring lands with the auto-attack loop.
            SourceAmotion = 500,
            TargetAmotion = 500,
            Damage = damage,
            IsSpDamage = 0,
            Div = 1,
            ActionType = damage > 0 ? DamageActionType.Normal : DamageActionType.Flee,
            Damage2 = 0,
        };
        // Broadcast to AOI of the target (where the visual lives). Source
        // is automatically in the same AOI for melee; long-range will fan
        // out from both src and dst when we add it.
        _visibility.SendToArea(target, packet);
    }

    private void HandleDeath(Entity target, Entity? source)
    {
        switch (target)
        {
            case MobEntity mob:
                // Re-uses MobSpawnService's death pipeline so respawn timer
                // wiring + visibility broadcast (Died reason) stay in one
                // place. KillMob also pulls drops once item_db lands.
                _mobSpawn.KillMob(mob.Id);
                _logger.LogInformation(
                    "Mob {Name} (#{Id}) died (source: {Source})",
                    mob.Name, mob.Id.Value, source?.Id.Value);
                break;

            case PlayerEntity pc:
                // PC death pipeline lands with MS3 savepoint warp. For
                // scaffolding: just clamp HP at 0 and broadcast vanish so
                // the corpse disappears.
                _visibility.NotifyVanishedToArea(pc, VanishReason.Died);
                _entities.Remove(pc.Id);
                _logger.LogInformation(
                    "PC {Name} (char {CharId}) died (source: {Source})",
                    pc.Name, pc.CharacterId, source?.Id.Value);
                break;
        }
    }
}
