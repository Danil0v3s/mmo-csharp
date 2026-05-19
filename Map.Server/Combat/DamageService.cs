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
    private readonly IBattleCalculator _battleCalc;
    private readonly Status.IExpService? _exp;
    private readonly ILogger<DamageService> _logger;

    public DamageService(
        IVisibilityService visibility,
        IMobSpawnService mobSpawn,
        IEntityRegistry entities,
        IBattleCalculator battleCalc,
        ILogger<DamageService> logger,
        Status.IExpService? exp = null)
    {
        _visibility = visibility;
        _mobSpawn = mobSpawn;
        _entities = entities;
        _battleCalc = battleCalc;
        _exp = exp;
        _logger = logger;
    }

    public int ApplyDamage(Entity target, int damage, Entity? source = null)
    {
        // Match rAthena clif_damage: 0 damage → miss animation (Flee),
        // >0 → normal swing. Callers with full BattleDamage in hand should
        // use PerformMeleeAttack so Critical/MultiHit aren't lost.
        var action = damage > 0
            ? Core.Server.Packets.Out.ZC.DamageActionType.Normal
            : Core.Server.Packets.Out.ZC.DamageActionType.Flee;
        return ApplyResolved(target, source, damage, action);
    }

    public BattleDamage PerformMeleeAttack(Entity source, Entity target)
    {
        var damage = _battleCalc.CalcWeaponAttack(source, target);
        // Even a miss broadcasts ZC_NOTIFY_ACT3 so the client animates
        // the swing + the dodge — rAthena: clif_damage(... DMG_FLEE ...)
        // even when total = 0.
        ApplyResolved(target, source, (int)Math.Clamp(damage.Total, 0, int.MaxValue), damage.Type);
        return damage;
    }

    private int ApplyResolved(
        Entity target,
        Entity? source,
        int damage,
        Core.Server.Packets.Out.ZC.DamageActionType action)
    {
        if (damage < 0) damage = 0;
        var (currentHp, _) = GetHp(target);
        var actual = Math.Min(damage, currentHp);
        SetHp(target, currentHp - actual);

        BroadcastAct(target, source, actual, action);

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

    private void BroadcastAct(Entity target, Entity? source, int damage, DamageActionType action)
    {
        // Pull amotion from the attacker's stats (renewal ASPD-derived).
        // Falls back to 500ms when there's no source (environmental damage).
        ushort srcAmotion = source?.Stats.Amotion ?? 500;
        ushort tgtDmotion = target.Stats.Dmotion;
        var packet = new ZC_NOTIFY_ACT3
        {
            SourceId = source?.Id.Value ?? 0,
            TargetId = target.Id.Value,
            ServerTick = (uint)Environment.TickCount,
            SourceAmotion = srcAmotion,
            TargetAmotion = tgtDmotion,
            Damage = damage,
            IsSpDamage = 0,
            Div = 1,
            ActionType = action,
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
                // EXP attribution — last-hit-wins for MS3 first slice
                // (rAthena's tdmg_id table is the next iteration). If the
                // last-hit attacker is a PC, award the mob's full base+job
                // exp pool before tearing the mob down.
                if (source is PlayerEntity killer && _exp != null && mob.DbEntry != null)
                {
                    _exp.GainExp(killer, mob.DbEntry.BaseExp, mob.DbEntry.JobExp);
                }
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
