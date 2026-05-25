using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Spawn;
using Map.Server.Status;
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
    // T2.4b+: SC consumers (SteelBody / Kyrie / AutoGuard) — optional so
    // older tests that build DamageService without the SC engine still work.
    private readonly IStatusChangeService? _sc;
    private readonly Random _rng;

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
        IServiceProvider? services = null,
        IStatusChangeService? sc = null,
        Random? rng = null)
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
        _sc = sc;
        _rng = rng ?? Random.Shared;
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

        // Wave 26 — SC_DEVOTION (Crusader Devotion). When the target has
        // an active Devotion link, the linked guard (Val1 = guard entity
        // id) takes the hit instead. Re-dispatches the ApplyResolved
        // call on the guard and short-circuits the target's HP delta.
        // rAthena reference: status.cpp SC_DEVOTION + battle_calc_damage
        // devotion check. The bookkeeping for guard distance / line-of-
        // sight enforcement lives on the SC application (already gated
        // before Val1 was stored), so the consumer just trusts Val1.
        if (_sc != null && target.Id.Value != 0)
        {
            var dev = _sc.Get(target, StatusType.Devotion);
            if (dev != null && dev.Val1 > 0)
            {
                var guardId = new EntityId(dev.Val1);
                if (guardId.Value != target.Id.Value
                    && _entities.Get(guardId) is Entity guard
                    && guard.Id.Value != source?.Id.Value)
                {
                    // Recurse on the guard with the same action; the guard
                    // is the new damage sink. Return the original target's
                    // delta as 0 (rAthena draws a hit anim on target but
                    // applies HP to guard).
                    BroadcastAct(target, source, 0, action);
                    return ApplyResolved(guard, source, damage, action);
                }
            }
        }

        // T2.4b+: SC overlays on incoming damage. Order matches rAthena
        // status_calc_damage:
        //   1) AutoGuard rolls full-block first (binary outcome).
        //   2) Kyrie's HP shield soaks damage up to its remaining pool.
        //   3) SteelBody applies a 90 % flat reduction last (multiplies
        //      whatever survived absorb).
        damage = ApplyScDamageReduction(target, damage, ref action);

        var (currentHp, _) = GetHp(target);
        var actual = Math.Min(damage, currentHp);
        SetHp(target, currentHp - actual);

        // T4.7 — DmgListLog (rAthena md->dmglog). Track every hit a mob
        // takes so MSC_ATTACKPCGT / MSC_ATTACKPCGE evaluators can answer
        // distinct-attacker queries, and so the EXP-share split at death
        // has the data it needs. Only meaningful for mob targets with a
        // known source.
        if (target is MobEntity dmgMob && source != null && actual > 0)
        {
            dmgMob.DmgList.Record(source.Id, actual);
        }
        // T5.1a — same log, PC side. Mirrors rAthena's
        // unit_counttargeted PC code path so MSC_MASTERATTACKED can read
        // the homun/merc master's attacker count.
        if (target is PlayerEntity dmgPc && source != null && actual > 0)
        {
            dmgPc.AttackerLog.Record(source.Id, actual);
        }

        BroadcastAct(target, source, actual, action);

        // P0.3 — SC post-resolve reflect / sacrifice consumers. Read the
        // target's reflect SCs and feed back a slice of the dealt damage
        // to the attacker. Runs AFTER BroadcastAct so the original hit
        // animates first, then the reflect (matches rAthena ordering).
        ApplyScPostResolve(target, source, actual);

        var remaining = currentHp - actual;
        if (remaining <= 0)
        {
            // Wave 30 — SC_NEN (NJ_NEN) auto-revive. When the lethal hit
            // lands and the target carries an active SC_NEN, consume the
            // SC to restore the target to 1 HP instead of dying. rAthena
            // ref: status.cpp SC_NEN consume-on-death branch.
            if (_sc != null && _sc.Get(target, StatusType.Nen) != null)
            {
                SetHp(target, 1);
                _sc.End(target, StatusType.Nen);
                return actual;
            }
            // Wave 33 — SC_KAIZEL (Soul Linker Kaizel). Val2 = % of MaxHp
            // to revive with (10*Val1). Same consume-on-death semantic.
            if (_sc != null)
            {
                var kaizel = _sc.Get(target, StatusType.Kaizel);
                if (kaizel != null && kaizel.Val2 > 0)
                {
                    var (_, max) = GetHp(target);
                    var reviveHp = Math.Max(1, max * kaizel.Val2 / 100);
                    SetHp(target, reviveHp);
                    _sc.End(target, StatusType.Kaizel);
                    return actual;
                }
            }
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

    /// <summary>
    /// Apply incoming damage reductions / absorbs from active SCs on the
    /// target. Mirrors the renewal status_calc_damage path that runs after
    /// battle_calc_attack returns and before status_damage commits HP loss.
    ///
    /// Mutates <paramref name="action"/> to <c>Flee</c> when the target
    /// fully blocks (Autoguard / Kyrie zero / SteelBody 100 % rounded) so
    /// the client renders a dodge animation rather than a hit.
    /// </summary>
    private int ApplyScDamageReduction(Entity target, int damage, ref DamageActionType action)
    {
        if (_sc == null || damage <= 0) return damage;

        // ---- AutoGuard ---------------------------------------------------
        // Val1 = block chance %. rAthena: 5+5*lv at start, scaled by AGI.
        // For the C# port we honor whatever the caster stored in Val1.
        var auto = _sc.Get(target, StatusType.Autoguard);
        if (auto != null && auto.Val1 > 0 && _rng.Next(100) < auto.Val1)
        {
            action = DamageActionType.Flee; // ZC renders the dodge anim.
            return 0;
        }

        // ---- Kyrie shield -----------------------------------------------
        // Val1 = remaining HP shield; Val2 = remaining hit counter.
        // Each hit consumes from both pools — SC ends when either hits 0.
        var kyrie = _sc.Get(target, StatusType.Kyrie);
        if (kyrie != null && kyrie.Val1 > 0 && kyrie.Val2 > 0)
        {
            var absorb = Math.Min(damage, kyrie.Val1);
            damage -= absorb;
            kyrie.Val1 -= absorb;
            kyrie.Val2 -= 1;
            if (kyrie.Val1 <= 0 || kyrie.Val2 <= 0)
            {
                _sc.End(target, StatusType.Kyrie);
            }
            if (damage <= 0)
            {
                action = DamageActionType.Flee;
                return 0;
            }
        }

        // ---- SteelBody ---------------------------------------------------
        // 90 % flat reduction on physical + magic. Always rounds down to
        // at least 1 so a hit still registers visually.
        if (_sc.Get(target, StatusType.Steelbody) != null)
        {
            damage = Math.Max(1, damage / 10);
        }

        // ---- Defender ---------------------------------------------------
        // P0.3 — rAthena Crusader Defender. Val1 stores the per-level
        // ranged-attack reduction (5+5*Val1 %). The SC's CalcFlag
        // already mutates Def via the generator default; here we apply
        // an additional incoming-damage reduction for ranged hits.
        // First slice: treat every hit as ranged for the read (combat
        // pipeline doesn't pass attack-range info to this method yet).
        // When the range param threads through, gate on RANGED only.
        var defender = _sc.Get(target, StatusType.Defender);
        if (defender != null && defender.Val1 > 0)
        {
            var defPct = 5 + 5 * defender.Val1;
            damage = Math.Max(1, damage - (damage * defPct / 100));
        }

        // ---- Aeterna ---------------------------------------------------
        // P0.3 — rAthena Priest Lex Aeterna (status.cpp:11297). Next
        // hit on the target deals doubled damage; SC ends on consume.
        // The doubling applies on the FIRST hit after attach, then SC
        // ends regardless of which path delivered the damage.
        if (_sc.Get(target, StatusType.Aeterna) != null)
        {
            damage *= 2;
            _sc.End(target, StatusType.Aeterna);
        }

        // ---- Providence -------------------------------------------------
        // Wave 26 — rAthena Crusader Providence (battle.cpp Providence
        // branch). Val1 = level; the per-race damage resist is read from
        // Val2 (rAthena's storage convention: race id of the resist) or
        // applies broadly when Val2 == 0 / matches the attacker's race.
        // Standard rAthena: 5*Val1 % resist vs Demon / Undead. We apply
        // it whenever the SC is active; race-specific gating lands when
        // attacker race is threaded through here.
        var providence = _sc.Get(target, StatusType.Providence);
        if (providence != null && providence.Val1 > 0)
        {
            var resistPct = 5 * providence.Val1;
            damage = Math.Max(1, damage - (damage * resistPct / 100));
        }

        // ---- Siegfried -------------------------------------------------
        // Wave 28 — rAthena <c>BD_SIEGFRIED</c> (status.cpp:10728-10731).
        // Val2 = elemental resistance %, Val3 = status ailment resist %.
        // The elemental-specific resist requires attacker element threading;
        // until then apply Val2 as a broad damage reduction (matches the
        // rAthena fallback path when the element resist matrix is sparse).
        var siegfried = _sc.Get(target, StatusType.Siegfried);
        if (siegfried != null && siegfried.Val2 > 0)
        {
            damage = Math.Max(1, damage - (damage * siegfried.Val2 / 100));
        }

        return damage;
    }

    /// <summary>
    /// P0.3 — post-resolve reflect / sacrifice / reflectdamage. Runs
    /// AFTER damage commits to the target so the attacker takes back
    /// a slice of the damage they dealt.
    ///
    /// rAthena reference: <c>battle_check_dmg_reflect</c> (battle.cpp)
    /// + Sacrifice devotion link (status.cpp). For now applies the
    /// reflect deltas directly; future iteration threads the reflect
    /// chain through <c>IBattleReflectService</c>.
    /// </summary>
    private void ApplyScPostResolve(Entity target, Entity? source, int damageDealt)
    {
        if (_sc == null || damageDealt <= 0 || source == null) return;

        // SC_REFLECTSHIELD — feed back Val2 % of damage to the attacker.
        // rAthena defaults Val2 to 10+3*val1 (Paladin), 15+5*val1 (Cardinal).
        // We read whatever the caster stored; combat-side consumer here.
        var reflect = _sc.Get(target, StatusType.Reflectshield);
        if (reflect != null && reflect.Val2 > 0 && source != target)
        {
            var back = damageDealt * reflect.Val2 / 100;
            if (back > 0) ApplyDamage(source, back, target);
        }

        // SC_REFLECTDAMAGE — Royal Guard reflect with cell-range scope.
        // First slice: same as Reflectshield (combat-side scope refinement
        // when the area-iterator threads through).
        var rdmg = _sc.Get(target, StatusType.Reflectdamage);
        if (rdmg != null && rdmg.Val2 > 0 && source != target)
        {
            var back = damageDealt * rdmg.Val2 / 100;
            if (back > 0) ApplyDamage(source, back, target);
        }

        // SC_DEATHBOUND — rAthena Death Bound. Val2 stores the
        // bounce-back % (500 + 100*val1). Triggers on melee hit.
        var dbnd = _sc.Get(target, StatusType.Deathbound);
        if (dbnd != null && dbnd.Val2 > 0 && source != target)
        {
            var back = damageDealt * dbnd.Val2 / 1000; // ‰ scale (500 = 50 %)
            if (back > 0) ApplyDamage(source, back, target);
            _sc.End(target, StatusType.Deathbound);
        }

        // SC_SACRIFICE — devotion-link sibling takes damage instead.
        // First slice: the devotion target itself loses HP per hit
        // (val2 counter decrements). Real devotion link plumbing lands
        // when the link service ports.
        var sacrifice = _sc.Get(target, StatusType.Sacrifice);
        if (sacrifice != null && sacrifice.Val2 > 0)
        {
            // val2 is the hit counter; subtract until exhausted.
            sacrifice.Val2 -= 1;
            if (sacrifice.Val2 <= 0)
            {
                _sc.End(target, StatusType.Sacrifice);
            }
        }
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
                    // DBR-1b: mob.Level threads to the per-recipient
                    // level-gap penalty (ILevelPenaltyService) inside
                    // GainExp — parity with rAthena pc.cpp:8186.
                    var srcLv = mob.Level;
                    // Try party share first (mob.cpp:mob_dead → party_exp_share).
                    if (_partyShare != null && _partyShare.ShareKill(killer, mob.DbEntry.BaseExp, mob.DbEntry.JobExp, srcLv))
                    {
                        awarded = true;
                    }
                    if (!awarded && _exp != null)
                    {
                        _exp.GainExp(killer, mob.DbEntry.BaseExp, mob.DbEntry.JobExp, srcLv);
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
