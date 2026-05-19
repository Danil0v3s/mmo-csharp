using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Spawn;
using Map.Server.Visibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Combat;

public sealed class DamageService : IDamageService
{
    private readonly IVisibilityService _visibility;
    // Narrow seam — see IMobDeathSink. Taking the full IMobSpawnService here
    // re-introduces the spawn → movement → warp → setpos → attack → damage
    // DI cycle and Map.Server can't construct its root scope.
    private readonly IMobDeathSink _mobSpawn;
    private readonly IEntityRegistry _entities;
    private readonly IBattleCalculator _battleCalc;
    private readonly Status.IExpService? _exp;
    private readonly IPcDeathService? _pcDeath;
    private readonly Party.IPartyShareService? _partyShare;
    private readonly Map.Server.World.IMapFlagService? _mapFlags;
    private readonly Map.Server.World.IMapWorldRegistry? _maps;
    // IMobAiService → IAttackService → IDamageService is a hard cycle at
    // construction time. Resolve through IServiceProvider so the back-edge
    // is established only when an actual NotifyAttacked call runs, well
    // after every singleton is built.
    private readonly IServiceProvider? _services;
    private Map.Server.Mob.IMobAiService? _cachedMobAi;
    private Map.Server.Mob.IMobAiService? MobAi
        => _cachedMobAi ??= _services?.GetService<Map.Server.Mob.IMobAiService>();
    private readonly ILogger<DamageService> _logger;

    public DamageService(
        IVisibilityService visibility,
        IMobDeathSink mobSpawn,
        IEntityRegistry entities,
        IBattleCalculator battleCalc,
        ILogger<DamageService> logger,
        Status.IExpService? exp = null,
        IPcDeathService? pcDeath = null,
        Party.IPartyShareService? partyShare = null,
        Map.Server.World.IMapFlagService? mapFlags = null,
        Map.Server.World.IMapWorldRegistry? maps = null,
        IServiceProvider? services = null)
    {
        _visibility = visibility;
        _mobSpawn = mobSpawn;
        _entities = entities;
        _battleCalc = battleCalc;
        _exp = exp;
        _pcDeath = pcDeath;
        _partyShare = partyShare;
        _mapFlags = mapFlags;
        _maps = maps;
        _services = services;
        _logger = logger;
    }

    public int ApplyDamage(Entity target, int damage, Entity? source = null)
    {
        if (source != null && !CanDamage(source, target)) return 0;
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
        if (!CanDamage(source, target)) return default;
        var damage = _battleCalc.CalcWeaponAttack(source, target);
        // Even a miss broadcasts ZC_NOTIFY_ACT3 so the client animates
        // the swing + the dodge — rAthena: clif_damage(... DMG_FLEE ...)
        // even when total = 0.
        ApplyResolved(target, source, (int)Math.Clamp(damage.Total, 0, int.MaxValue), damage.Type);
        return damage;
    }

    /// <summary>
    /// Mirror of rAthena <c>battle_check_target</c> (battle.cpp:9450) —
    /// returns false when:
    ///   * source and target share party / guild (friendly fire off),
    ///   * the source map has <c>nopvp</c> set (PC ↔ PC damage refused).
    /// PvE (PC vs Mob / Mob vs PC / Mob vs Mob) is always allowed.
    /// GvG zones are documented under <see cref="MapFlag.Gvg"/> but the
    /// full GvG matrix lands when WoE ports — for now we only enforce
    /// the no-friendly-fire / nopvp pair.
    /// </summary>
    private bool CanDamage(Entity source, Entity target)
    {
        // rAthena status_damage gates on invincible_timer first — a PC
        // mid-warp window can't take damage from any source (PvE or PvP).
        if (target is PlayerEntity pc
            && pc.InvincibleUntilTick > Environment.TickCount64)
        {
            return false;
        }

        if (source is not PlayerEntity src || target is not PlayerEntity dst) return true;
        if (src.Id == dst.Id) return true;

        // Same party / guild → never hurt each other outside GvG.
        if (src.PartyId != 0 && src.PartyId == dst.PartyId) return false;
        if (src.GuildId != 0 && src.GuildId == dst.GuildId) return false;

        // Source map's nopvp refuses any PC↔PC damage.
        if (_mapFlags != null && _maps != null)
        {
            var map = _maps.All.FirstOrDefault(m => (uint)m.Name.GetHashCode() == src.MapId);
            if (map != null && _mapFlags.IsSet(map.Name, Map.Server.World.MapFlag.NoPvp))
            {
                return false;
            }
        }
        return true;
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
        else if (target is MobEntity targetMob && source != null && MobAi is { } ai)
        {
            // rAthena mob_damage (mob.cpp:1743): each surviving hit feeds
            // the rude-attacked counter so unreachable attackers can't
            // chip a mob indefinitely.
            ai.NotifyAttacked(targetMob, source);
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
                if (source is PlayerEntity killer && mob.DbEntry != null)
                {
                    var awarded = false;
                    // Try party share first (mob.cpp:mob_dead → party_exp_share).
                    if (_partyShare != null && _partyShare.ShareKill(killer, mob.DbEntry.BaseExp, mob.DbEntry.JobExp))
                    {
                        awarded = true;
                    }
                    if (!awarded && _exp != null)
                    {
                        _exp.GainExp(killer, mob.DbEntry.BaseExp, mob.DbEntry.JobExp);
                    }
                }
                // Re-uses MobSpawnService's death pipeline so respawn timer
                // wiring + visibility broadcast (Died reason) stay in one
                // place. Drops attribute to the last-hitter (and their
                // party) for the loot-protection windows.
                _mobSpawn.KillMob(mob.Id, source as PlayerEntity);
                _logger.LogInformation(
                    "Mob {Name} (#{Id}) died (source: {Source})",
                    mob.Name, mob.Id.Value, source?.Id.Value);
                break;

            case PlayerEntity pc:
                if (_pcDeath != null)
                {
                    _pcDeath.OnPcDead(pc, source);
                }
                else
                {
                    // Pre-PcDeathService fallback: vanish + remove (loses
                    // the corpse step but keeps the AOI clean). Used by a
                    // handful of older tests that bypass the death service.
                    _visibility.NotifyVanishedToArea(pc, VanishReason.Died);
                    _entities.Remove(pc.Id);
                    _logger.LogInformation(
                        "PC {Name} (char {CharId}) died (source: {Source})",
                        pc.Name, pc.CharacterId, source?.Id.Value);
                }
                break;
        }
    }
}
