using Map.Server.Status;

namespace Map.Server.Entities;

/// <summary>
/// A player character on a map. The single source of truth for "this account
/// is on this map at this cell." Replaces the legacy struct of the same name
/// that lived in MapServerImpl.cs.
///
/// EntityId == CharacterId for PCs (rAthena convention; char_id is globally
/// unique so it doubles as the block_list id).
/// </summary>
public sealed class PlayerEntity : Entity
{
    public int AccountId { get; }
    public int CharacterId => Id.Value;
    public string Name { get; }
    public Guid SessionId { get; }

    /// <summary>
    /// Current HP. Backed by <see cref="Entity.Stats"/>; mutated by the
    /// combat / skill / item paths. Hydrated from char-side persistence
    /// at session enter once the inventory/status IPC lands the full
    /// stats payload — until then defaults to the renewal Lv1 baseline.
    /// </summary>
    public int Hp
    {
        get => Stats.Hp;
        set => Stats.Hp = value;
    }

    /// <summary>
    /// Maximum HP. Backed by <see cref="Entity.Stats"/>; written by
    /// <c>IStatusCalcService</c> when the stat block is rebuilt.
    /// </summary>
    public int MaxHp
    {
        get => Stats.MaxHp;
        set => Stats.MaxHp = value;
    }

    /// <summary>Current SP. Backed by <see cref="Entity.Stats"/>.</summary>
    public int Sp
    {
        get => Stats.Sp;
        set => Stats.Sp = value;
    }

    /// <summary>Maximum SP. Backed by <see cref="Entity.Stats"/>.</summary>
    public int MaxSp
    {
        get => Stats.MaxSp;
        set => Stats.MaxSp = value;
    }

    /// <summary>Persisted base / job EXP. Mutated by <c>IExpService.GainExp</c>; saved by autosave.</summary>
    public long BaseExp { get; set; }
    public long JobExp { get; set; }

    /// <summary>Persisted Job level — base level lives on <see cref="Entity.Level"/>.</summary>
    public int JobLevel { get; set; } = 1;

    /// <summary>Unspent stat / skill points. <c>pc_checkbaselevelup</c> awards status points; job levelup awards skill points.</summary>
    public int StatusPoints { get; set; }
    public int SkillPoints { get; set; }

    /// <summary>Party id, 0 = solo. Sourced from char-server at session enter.</summary>
    public int PartyId { get; set; }

    /// <summary>Guild id, 0 = none. Sourced from char-server at session enter.</summary>
    public int GuildId { get; set; }

    public override EntityType Type => EntityType.Pc;

    public PlayerEntity(
        int characterId,
        int accountId,
        string name,
        Guid sessionId,
        uint mapId,
        short x,
        short y)
        : base(new EntityId(characterId), mapId, x, y)
    {
        AccountId = accountId;
        Name = name ?? string.Empty;
        SessionId = sessionId;
        // Renewal Lv1 Novice baseline so any entity that bypasses the calc
        // service still has plausible HP/SP. status_calc_pc overwrites these.
        Stats.MaxHp = 40;
        Stats.Hp = 40;
        Stats.MaxSp = 11;
        Stats.Sp = 11;
        Stats.Race = BattleRace.PlayerHuman;
    }
}
