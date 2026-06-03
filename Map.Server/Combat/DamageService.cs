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
    // FEATURE-01 — mob-death fan-out (quest/achievement/pet-catch/MVP). Runs before KillMob frees the mob.
    private readonly Mob.IMobDeathObserver? _mobDeath;
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
    // COMBAT-08 — damage-driven cast interrupt. The cast service /
    // skill-result broadcaster / skill_db all sit downstream of the attack
    // pipeline, so (like MobAi) resolve them lazily through IServiceProvider
    // to avoid the construction-time DI cycle.
    private Map.Server.Skills.ISkillCastService? _cachedCast;
    private Map.Server.Skills.ISkillCastService? CastSvc
        => _cachedCast ??= _services?.GetService<Map.Server.Skills.ISkillCastService>();
    private Map.Server.Skills.ISkillClientService? _cachedSkillClient;
    private Map.Server.Skills.ISkillClientService? SkillClientSvc
        => _cachedSkillClient ??= _services?.GetService<Map.Server.Skills.ISkillClientService>();
    private Map.Server.Skills.ISkillDb? _cachedSkillDb;
    private Map.Server.Skills.ISkillDb? SkillDbSvc
        => _cachedSkillDb ??= _services?.GetService<Map.Server.Skills.ISkillDb>();
    // COMBAT-25 — defensive ground-unit intercept (Safety Wall / Pneuma).
    private Map.Server.Skills.ISkillUnitService? _cachedUnits;
    private Map.Server.Skills.ISkillUnitService? UnitSvc
        => _cachedUnits ??= _services?.GetService<Map.Server.Skills.ISkillUnitService>();
    private readonly ILogger<DamageService> _logger;
    // T2.4b+: SC consumers (SteelBody / Kyrie / AutoGuard) — optional so
    // older tests that build DamageService without the SC engine still work.
    // COMBAT-31: held lazily to break the DI cycle DamageService ↔
    // StatusChangeService (StatusChangeService injects IDamageService for DoT
    // periodic damage; DamageService reads SC state for reflect/Devotion/
    // Kaahi/Deadly-Infect/etc.). Every SC read here is at damage time, never at
    // construction, so deferring the resolve to first `.Value` makes the
    // constructor graph acyclic. The `_sc` property keeps every call site
    // below unchanged (the Lazy caches after the first resolve).
    private readonly Lazy<IStatusChangeService>? _scLazy;
    private IStatusChangeService? _sc => _scLazy?.Value;
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
        Lazy<IStatusChangeService>? sc = null,
        Random? rng = null,
        IZoneDamageService? zone = null,
        Mob.IMobDeathObserver? mobDeath = null)
    {
        _visibility = visibility;
        _mobSpawn = mobSpawn;
        _entities = entities;
        _battleCalc = battleCalc;
        _exp = exp;
        _pcDeath = pcDeath;
        _partyShare = partyShare;
        _mobDeath = mobDeath;
        _mapFlags = mapFlags;
        _maps = maps;
        _services = services;
        _scLazy = sc;
        _rng = rng ?? Random.Shared;
        _logger = logger;
        _zone = zone;
    }

    // COMBAT-20 — GvG/BG zone scaling for the weapon auto-attack final stage.
    private readonly IZoneDamageService? _zone;

    public int ApplyDamage(Entity target, int damage, Entity? source = null, int hits = 1)
    {
        if (source != null && !CanDamage(source, target)) return 0;
        // Match rAthena clif_damage: 0 damage → miss animation (Flee),
        // >0 → normal swing. Callers with full BattleDamage in hand should
        // use PerformMeleeAttack so Critical/MultiHit aren't lost.
        var action = damage > 0
            ? Core.Server.Packets.Out.ZC.DamageActionType.Normal
            : Core.Server.Packets.Out.ZC.DamageActionType.Flee;
        // COMBAT-17 — skill callers pass the skill's hit count (Sonic Blow
        // div = 8, etc.) so ZC_NOTIFY_ACT3.Div renders the right number of
        // hits. `damage` is the already-resolved total (the skill ratio
        // produces the full multi-hit total; this is rAthena's negative-div
        // "single damage shown as N hits" — no further multiplication).
        return ApplyResolved(target, source, damage, action, hits);
    }

    public BattleDamage PerformMeleeAttack(Entity source, Entity target)
    {
        if (!CanDamage(source, target)) return default;
        var damage = _battleCalc.CalcWeaponAttack(source, target);
        // COMBAT-25 — defensive ground-unit intercept. A landed swing on a
        // Safety Wall cell (melee) or Pneuma cell (ranged) is fully blocked.
        if (damage.DidHit && damage.Total > 0
            && (TryGroundUnitBlock(target, BattleCalculator.IsShortRange(source))
                || IsBasilicaImmune(target, source)))
        {
            damage.Damage = 0;
            damage.Damage2 = 0;
        }
        // COMBAT-20 — auto-attack final stage: plant clamp (MD_IGNOREMELEE /
        // MD_IGNORERANGED → 1) + GvG/BG range rate (normal attack = BF_SHORT/LONG).
        // Weapon SKILLS apply their plant/zone stage post-ratio (COMBAT-42).
        // COMBAT-42: pass _sc so SC_INVINCIBLE clamps the auto-attack too.
        BattleCalculator.ApplyPlantAndZone(damage, source, target,
            BattleCalculator.IsShortRange(source), isSkill: false, _zone, _sc);
        // Even a miss broadcasts ZC_NOTIFY_ACT3 so the client animates
        // the swing + the dodge — rAthena: clif_damage(... DMG_FLEE ...)
        // even when total = 0.
        // COMBAT-18 — the HP delta is the combined total (right + left); the
        // wire splits the right/left numbers via damage2.
        ApplyResolved(target, source, (int)Math.Clamp(damage.Total, 0, int.MaxValue), damage.Type, damage.Hits,
            (int)Math.Clamp(damage.Damage2, 0, int.MaxValue));

        // COMBAT-44 — on-hit HP/SP vanish (bonus2 bHPVanishRate / bSPVanishRate,
        // rAthena battle.cpp:9941): a landed weapon swing rolls the rate and drains
        // per% of the target's MAX HP/SP.
        ApplyVanish(source, target, damage.Total > 0);

        // Wave 66 + 69 / Track B — apply dmotion + walkdelay on the
        // target via the canonical IAttackService.SetAttackDelay +
        // IMovementService.SetWalkDelay entry points. Both are resolved
        // lazily through IServiceProvider because IAttackService ←
        // IDamageService forms a registration cycle.
        if (damage.DMotion > 0)
        {
            var atk = _services?.GetService(typeof(IAttackService)) as IAttackService;
            atk?.SetAttackDelay(target, damage.DMotion);
        }
        if (damage.WalkDelay > 0)
        {
            var mov = _services?.GetService(typeof(Movement.IMovementService)) as Movement.IMovementService;
            mov?.SetWalkDelay(target, damage.WalkDelay);
        }
        return damage;
    }

    /// <summary>
    /// Mirror of rAthena <c>battle_check_target</c> (battle.cpp:9450) —
    /// returns false when:
    ///   * source and target share party / guild (friendly fire off),
    ///   * the source map has <c>nopvp</c> set (PC ↔ PC damage refused).
    /// <summary>
    /// COMBAT-44 — rAthena <c>battle_vanish</c> (battle.cpp:9933): a PC weapon hit
    /// rolls the HP/SP vanish rate and drains a percentage of the target's MAX
    /// HP/SP. The roll is in 1/1000 units (rate ≥ 1000 = guaranteed).
    /// </summary>
    private void ApplyVanish(Entity source, Entity target, bool hit)
    {
        if (!hit || source is not PlayerEntity pc || pc.EquipBonuses is not { } eq) return;

        // COMBAT-83 — the attack's BF_* flag for the flag-gated vanish (bonus3 bHPVanishRate,…,bf).
        // A weapon hit: BF_WEAPON + melee/ranged from the attacker's range + normal|skill.
        int attackFlag = Map.Server.Status.BattleFlags.Weapon
            | (pc.Stats.AttackRange > 2 ? Map.Server.Status.BattleFlags.Long : Map.Server.Status.BattleFlags.Short)
            | Map.Server.Status.BattleFlags.Normal | Map.Server.Status.BattleFlags.Skill;

        if (eq.HpVanishPer != 0 && VanishFlagMatches(eq.HpVanishFlag, attackFlag) && VanishRoll(eq.HpVanishRate))
        {
            var drain = (int)((long)target.Stats.MaxHp * eq.HpVanishPer / 100);
            if (drain > 0) ApplyDamage(target, drain, source);
        }
        if (eq.SpVanishPer != 0 && target.Stats.MaxSp > 0 && VanishFlagMatches(eq.SpVanishFlag, attackFlag) && VanishRoll(eq.SpVanishRate))
        {
            var drain = (int)((long)target.Stats.MaxSp * eq.SpVanishPer / 100);
            if (drain > 0) target.Stats.Sp = Math.Max(0, target.Stats.Sp - drain);
        }
    }

    /// <summary>COMBAT-83 — flag 0 (the COMBAT-44 bonus2 form) is unconstrained; otherwise the
    /// attack must match the gated BF flag (rAthena s_vanish_bonus.flag).</summary>
    private static bool VanishFlagMatches(int vanishFlag, int attackFlag)
        => vanishFlag == 0 || Map.Server.Status.BattleFlags.Matches(vanishFlag, attackFlag);

    private bool VanishRoll(int rate) => rate > 0 && (rate >= 1000 || _rng.Next(1000) < rate);

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

        // NOTE: this gate covers ATTACK damage. ApplyDamage is also the path
        // for mechanic-damage that must NOT be allegiance-gated (Akaitsuki
        // heal-flip → Heal.cs:162, reflect, DoT applied to allies/self), so we
        // keep the permissive PC↔PC default here. Routing the *attack* path
        // through the shared BattleTargetResolver (and splitting attack vs
        // mechanic-damage so the latter bypasses it) is SKILL-16. The splash
        // victim filter already uses BattleTargetResolver, so it never feeds a
        // friendly/neutral victim into this gate.
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

    /// <summary>
    /// COMBAT-27 — rAthena <c>map_flag_gvg2(m) || MF_BATTLEGROUND</c> for the
    /// no-cast-cancel gate. The GvG-gated <c>bNoCastCancel</c> + SC exemptions
    /// only apply on normal maps, NOT in WoE / Battleground zones.
    /// </summary>
    private bool IsGvgOrBgMap(Entity who)
    {
        if (_mapFlags == null || _maps == null) return false;
        var map = _maps.All.FirstOrDefault(m => (uint)m.Name.GetHashCode() == who.MapId);
        if (map == null) return false;
        return _mapFlags.IsSet(map.Name, Map.Server.World.MapFlag.Gvg)
            || _mapFlags.IsSet(map.Name, Map.Server.World.MapFlag.Battleground);
    }

    private int ApplyResolved(
        Entity target,
        Entity? source,
        int damage,
        Core.Server.Packets.Out.ZC.DamageActionType action,
        int hits = 1,
        int damage2 = 0)
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
                    BroadcastAct(target, source, 0, action, hits, damage2);
                    return ApplyResolved(guard, source, damage, action, hits, damage2);
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

        BroadcastAct(target, source, actual, action, hits, damage2);

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
            // COMBAT-08 — rAthena status_damage on death calls
            // unit_skillcastcancel(target,0): a dying caster's cast is
            // dropped unconditionally (no CastCancel gate).
            InterruptCastOnDamage(target, onDeath: true);
            HandleDeath(target, source);
        }
        else
        {
            // COMBAT-08 — rAthena status_fix_damage survivor path calls
            // unit_skillcastcancel(target,2): a cancellable-on-hit cast is
            // interrupted when the target takes real damage.
            if (actual > 0) InterruptCastOnDamage(target, onDeath: false);

            if (target is MobEntity targetMob && source != null && MobAi is { } ai)
            {
                // rAthena mob_damage (mob.cpp:1743): each surviving hit feeds
                // the rude-attacked counter so unreachable attackers can't
                // chip a mob indefinitely.
                ai.NotifyAttacked(targetMob, source);
            }
        }
        return actual;
    }

    /// <summary>
    /// COMBAT-08 — abort an in-progress cast when the caster takes damage.
    /// Mirrors rAthena <c>unit_skillcastcancel(bl, type)</c> (unit.cpp:3107):
    /// <list type="bullet">
    /// <item><paramref name="onDeath"/> == false → the survivor "damage"
    /// variant (<c>type&amp;2</c>): only skills flagged <c>CastCancel</c> are
    /// interrupted, and a <c>bNoCastCancel</c> caster is exempt.</item>
    /// <item><paramref name="onDeath"/> == true → the death variant
    /// (<c>type==0</c>): the cast is dropped unconditionally.</item>
    /// </list>
    /// Resolves the cast / skill-result / skill_db services lazily (DI cycle).
    /// </summary>
    private void InterruptCastOnDamage(Entity target, bool onDeath)
    {
        var castSvc = CastSvc;
        if (castSvc == null || target.Id.Value == 0) return;
        if (!castSvc.IsCasting(target.Id)) return;

        if (!onDeath)
        {
            // Damage-interrupt gate (unit_skillcastcancel type 2).
            var (skillId, _) = castSvc.GetCurrentCast(target.Id);
            if (SkillDbSvc is { } db && !db.GetCastCancel(skillId)) return; // not cancellable by damage
            // COMBAT-27 — rAthena unit_skillcastcancel (unit.cpp) no-cancel gate:
            //   exempt when no_castcancel2 (unconditional), OR
            //   ((SC_UNLIMITEDHUMMINGVOICE || no_castcancel) && !gvg && !bg).
            // (SC_BASILICA is NOT a cast-cancel exemption in this rAthena — a
            // Basilica caster simply takes no damage, so this gate isn't reached.)
            if (target is PlayerEntity pc)
            {
                if (pc.EquipBonuses.NoCastCancel2) return; // unconditional
                bool noCancel = pc.EquipBonuses.NoCastCancel
                    || (_sc?.Get(pc, StatusType.Unlimitedhummingvoice) != null);
                if (noCancel && !IsGvgOrBgMap(pc)) return; // exempt on normal maps only
            }
        }

        if (castSvc.CancelCast(target.Id))
            SkillClientSvc?.BroadcastSkillCastCancel(target);
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

        // ---- Kaupe (SL_KAAHI family dodge) -------------------------------
        // SC-04 — rAthena battle.cpp:1555: roll Val2 % to FULLY block one
        // incoming hit (skill or normal); Val3 is the remaining block count
        // (decrement, end the SC at 0 — like a 1-charge Safety Wall).
        // status.cpp:11152 stores Val2 = 33*lv (+1 at lv3 = 100%), Val3 = count.
        // Checked first so a successful Kaupe pre-empts every other reduction.
        var kaupe = _sc.Get(target, StatusType.Kaupe);
        if (kaupe != null && kaupe.Val2 > 0 && _rng.Next(100) < kaupe.Val2)
        {
            kaupe.Val3 -= 1;
            if (kaupe.Val3 <= 0) _sc.End(target, StatusType.Kaupe);
            action = DamageActionType.Flee; // ZC renders the dodge anim.
            return 0;
        }

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

        // ---- Kaahi (Soul Linker heal-on-hit) ----------------------------
        // SC-04 — rAthena battle.cpp:10544: when the Kaahi-bearer is struck
        // and is below max HP, charge Val3 SP and restore up to Val2 HP.
        // status.cpp:11201 stores Val2 = 200*lv (heal), Val3 = 5*lv (SP cost).
        // No-op when SP is insufficient; gated on a LIVING target so a lethal
        // hit can't be "healed" back to life (death resolves after this).
        var kaahi = _sc.Get(target, StatusType.Kaahi);
        if (kaahi != null && kaahi.Val2 > 0)
        {
            var (curHp, maxHp) = GetHp(target);
            if (curHp > 0 && curHp < maxHp && target.Stats.Sp >= kaahi.Val3)
            {
                target.Stats.Sp -= kaahi.Val3;
                var heal = Math.Min(maxHp - curHp, kaahi.Val2);
                if (heal > 0) SetHp(target, curHp + heal);
            }
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

        // SC-08 — SC__DEADLYINFECT (Shadow Chaser Deadly Infect). rAthena
        // battle.cpp:1903/1940: on a melee hit dealing damage, roll
        // 30 + 10*val1 % to propagate the SCF_SPREADEFFECT DoTs in BOTH
        // directions — the bearer's spread-flagged SCs jump to the other party.
        // If the ATTACKER has it, spread attacker→target; if the TARGET has it,
        // spread target→attacker. (Melee-only `BF_SHORT` gating isn't threaded
        // into this post-resolve hook yet — same limitation as Defender/
        // Deathbound; tracked in COMBAT-25's range-threading.)
        if (source != target)
        {
            var srcInfect = _sc.Get(source, StatusType.Deadlyinfect);
            if (srcInfect != null && _rng.Next(100) < 30 + 10 * srcInfect.Val1)
                _sc.Spread(source, target);

            var tgtInfect = _sc.Get(target, StatusType.Deadlyinfect);
            if (tgtInfect != null && _rng.Next(100) < 30 + 10 * tgtInfect.Val1)
                _sc.Spread(target, source);
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

    /// <summary>
    /// COMBAT-25 — defensive ground-unit intercept. Returns true (and zeroes the
    /// caller's damage) when a unit on the target's cell blocks this attack:
    /// <b>Safety Wall</b> blocks melee (<paramref name="isShortRange"/>) hits and
    /// consumes one block from the group's pool (<c>group.Val2</c>), removing the
    /// group when spent; <b>Pneuma</b> blocks ranged hits (no pool). rAthena
    /// battle_calc_damage MG_SAFETYWALL / AL_PNEUMA cell checks.
    /// </summary>
    /// <summary>
    /// COMBAT-25/47 — a Safety Wall cell blocks a melee swing (consuming one block
    /// from the pool); a Pneuma cell blocks a ranged swing. Called by the auto-attack
    /// (PerformMeleeAttack) and the skill funnel (SkillAttackService) so a melee/ranged
    /// SKILL is intercepted too. Returns true when the hit is blocked.
    /// </summary>
    public bool TryGroundUnitBlock(Entity target, bool isShortRange)
    {
        var svc = UnitSvc;
        if (svc == null) return false;
        var units = svc.GetUnitsInArea(target.MapId, target.X, target.Y, 0);
        for (var i = 0; i < units.Count; i++)
        {
            var g = units[i].Group;
            if (isShortRange && g.SkillId == Map.Server.Skills.SkillIds.MG_SAFETYWALL)
            {
                // Consume one block; remove the wall when the pool is exhausted.
                g.Val2--;
                if (g.Val2 <= 0) svc.DelUnitGroup(g);
                return true;
            }
            if (!isShortRange && g.SkillId == Map.Server.Skills.SkillIds.AL_PNEUMA)
                return true;
        }
        return false;
    }

    /// <summary>
    /// COMBAT-49 — rAthena <c>battle_calc_damage</c>: a target with <c>SC_BASILICA_CELL</c>
    /// takes no damage from an attack, unless the attacker has <c>MD_STATUSIMMUNE</c> mode
    /// (boss/MVP). The <c>SP_SOULEXPLOSION</c> skill exemption is the caller's responsibility
    /// (only the skill funnel knows the skill id).
    ///
    /// <para><b>COMBAT-68 — dormant in renewal by design.</b> <c>SC_BASILICA_CELL</c> is granted
    /// only by <c>pc_cell_basilica</c> off a <c>CELL_BASILICA</c> cell, and that cell-marking
    /// (skill.cpp:21830) is <c>#ifndef RENEWAL</c> — so in renewal HP_BASILICA never marks a cell
    /// and this SC is never applied. The check is therefore inert in renewal (it stays correct
    /// for any hand-applied/scripted SC_BASILICA_CELL, and would light up if pre-renewal Basilica
    /// were ever ported). The renewal Basilica effect is the SC_BASILICA self-buff — see
    /// <c>Acolyte/Basilica.cs</c> + COMBAT-87.</para>
    /// </summary>
    public bool IsBasilicaImmune(Entity target, Entity source)
    {
        if (_sc == null) return false;
        if (_sc.Get(target, Map.Server.Status.StatusType.BasilicaCell) == null) return false;
        return (source.Stats.Mode & Map.Server.Status.MobMode.StatusImmune) == 0;
    }

    private void BroadcastAct(Entity target, Entity? source, int damage, DamageActionType action, int hits = 1, int damage2 = 0)
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
            // COMBAT-18 — the wire splits the hit into right (Damage) + left
            // (Damage2); `damage` here is the combined HP delta, so the
            // right-hand number is the remainder after the off-hand portion.
            Damage = Math.Max(0, damage - damage2),
            IsSpDamage = 0,
            // COMBAT-17 — div_ is the displayed hit count (rAthena
            // clif_damage's `div` arg). The client splits Damage across this
            // many hits; the HP delta is the full Damage either way.
            Div = (short)Math.Max(1, hits),
            ActionType = action,
            // COMBAT-18 — left-hand (dual-wield) damage; 0 for single-weapon /
            // skill / environmental hits.
            Damage2 = damage2,
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
                // FEATURE-01 — fan the death out to quest / achievement / pet-catch / MVP BEFORE
                // KillMob frees the mob + its damage log (rAthena runs these inside mob_dead while the
                // unit is still alive). Exp/drops are already awarded above + inside KillMob.
                _mobDeath?.OnMobDead(mob, source as PlayerEntity, mob.DmgList.Snapshot());

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
