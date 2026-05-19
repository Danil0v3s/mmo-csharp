using Map.Server.Entities;

namespace Map.Server.Status;

/// <summary>
/// Per-SC behavior table. Mirrors rAthena's giant <c>status.cpp</c>
/// switch statements collapsed into a record per SC type. Effects
/// register their <see cref="OnStart"/> (apply stat mods + initial
/// per-tick gating) and <see cref="OnEnd"/> (revert stat mods)
/// callbacks. Periodic logic is owned by the SC handler via the
/// <c>NextTick</c> / <c>PeriodMs</c> fields on <see cref="StatusChange"/>.
/// </summary>
public sealed class StatusEffectRegistry
{
    private readonly Dictionary<StatusType, StatusEffectHandler> _handlers = new();

    public StatusEffectRegistry()
    {
        // --- Built-in renewal SCs ported so the engine isn't empty at boot ---

        // SC_POISON — 1.5%/sec MaxHp DoT, 30s default.
        // rAthena status_change_start: SC_POISON sets tick = 1500.
        Register(StatusType.Poison, new StatusEffectHandler(
            OnStart: (_, _, _) => { /* no immediate stat mod */ },
            OnEnd: (_, _) => { },
            PeriodMs: 1500,
            OnPeriodic: (target, sc, applyDamage) =>
            {
                // Damage per tick = max(1, maxhp * 1.5%).
                var dmg = Math.Max(1, target.Stats.MaxHp * 15 / 1000);
                applyDamage(dmg);
            }));

        // SC_BLESSING — +val1 STR/INT/DEX (renewal). Stat mods applied
        // on start, reverted on end.
        Register(StatusType.Blessing, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Str = (short)Math.Min(short.MaxValue, target.Stats.Str + sc.Val1);
                target.Stats.IntStat = (short)Math.Min(short.MaxValue, target.Stats.IntStat + sc.Val1);
                target.Stats.Dex = (short)Math.Min(short.MaxValue, target.Stats.Dex + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Str = (short)Math.Max(0, target.Stats.Str - sc.Val1);
                target.Stats.IntStat = (short)Math.Max(0, target.Stats.IntStat - sc.Val1);
                target.Stats.Dex = (short)Math.Max(0, target.Stats.Dex - sc.Val1);
            }));

        // SC_INCREASEAGI — +val1 AGI, +ASPD%.
        Register(StatusType.IncreaseAgi, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - sc.Val1);
            }));

        // SC_DECREASEAGI — −val1 AGI (debuff, mirror of IncreaseAgi).
        Register(StatusType.DecreaseAgi, new StatusEffectHandler(
            OnStart: (target, sc, _) =>
            {
                target.Stats.Agi = (short)Math.Max(0, target.Stats.Agi - sc.Val1);
            },
            OnEnd: (target, sc) =>
            {
                target.Stats.Agi = (short)Math.Min(short.MaxValue, target.Stats.Agi + sc.Val1);
            }));

        // SC_HEAL_OVERTIME — val1 HP per tick, tick every 1s. Generic
        // heal-over-time anchor for items / future skills.
        Register(StatusType.HealOverTime, new StatusEffectHandler(
            OnStart: (_, _, _) => { },
            OnEnd: (_, _) => { },
            PeriodMs: 1000,
            OnPeriodic: (target, sc, _) =>
            {
                target.Stats.Hp = Math.Min(target.Stats.MaxHp, target.Stats.Hp + sc.Val1);
            }));
    }

    public void Register(StatusType type, StatusEffectHandler handler) => _handlers[type] = handler;

    public StatusEffectHandler? Get(StatusType type) => _handlers.GetValueOrDefault(type);
}

/// <summary>
/// One SC's behavior table.
/// <para><see cref="OnStart"/> runs after <see cref="StatusChange"/> is
/// attached; mutate <c>target.Stats</c> here.</para>
/// <para><see cref="OnEnd"/> runs before the SC is removed — must revert
/// any stat mods <see cref="OnStart"/> applied.</para>
/// <para><see cref="OnPeriodic"/> fires every <see cref="PeriodMs"/> ms
/// while the SC is active. The <c>applyDamage</c> callback bridges to
/// <see cref="Combat.IDamageService"/> so DoT effects route HP loss +
/// death + broadcast through the same pipeline as combat.</para>
/// </summary>
public sealed record StatusEffectHandler(
    Action<Entity, StatusChange, Entity?> OnStart,
    Action<Entity, StatusChange> OnEnd,
    int PeriodMs = 0,
    Action<Entity, StatusChange, Action<int>>? OnPeriodic = null);
