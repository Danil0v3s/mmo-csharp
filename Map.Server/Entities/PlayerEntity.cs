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

    /// <summary>True if the PC has invoked pc_setsit. Drives sitting regen bonus + action gates.</summary>
    public bool IsSitting { get; set; }

    /// <summary>
    /// rAthena <c>sc.option</c> — 32-bit effect-state bitmask
    /// (CART/RIDING/FALCON/MADOGEAR/INVISIBLE/...). Drives client
    /// sprite + several gameplay checks (overweight gates, mount-only
    /// skill access). Mutated via <see cref="Status.IPlayerOptionService"/>
    /// so the wire broadcast stays consistent.
    /// </summary>
    public Status.PlayerOption Option { get; set; }

    /// <summary>
    /// rAthena <c>sc.opt1</c> — OPT1 group (STONE/FREEZE/STUN/SLEEP).
    /// Broadcast as <c>bodyState</c> in <c>ZC_STATE_CHANGE3</c>.
    /// </summary>
    public ushort Opt1 { get; set; }

    /// <summary>
    /// rAthena <c>sc.opt2</c> — OPT2 group (POISON/CURSE/SILENCE/...).
    /// Broadcast as <c>healthState</c> in <c>ZC_STATE_CHANGE3</c>.
    /// </summary>
    public ushort Opt2 { get; set; }

    /// <summary>Karma flag — broadcast in <c>ZC_STATE_CHANGE3.PkMode</c>.</summary>
    public byte Karma { get; set; }

    /// <summary>
    /// rAthena <c>spiritball</c> — Monk / Sura sphere counter
    /// (Critical Explosion / Asura input). Capped at <c>MAX_SPIRITBALL</c>
    /// (rAthena default 15) by the caller.
    /// </summary>
    public int SpiritBall { get; set; }
    /// <summary>rAthena <c>soulball</c> — Soul Reaper soul count.</summary>
    public int SoulBall { get; set; }
    /// <summary>rAthena <c>servantball</c> — Servant Weapon count (Cross Slash).</summary>
    public int ServantBall { get; set; }
    /// <summary>rAthena <c>abyssball</c> — Abyss Chaser orb count.</summary>
    public int AbyssBall { get; set; }

    /// <summary>
    /// rAthena <c>spiritcharm</c> — Kagerou/Oboro element charm count.
    /// Cap (MAX_SPIRITCHARM = 10) enforced by the caller; orb service
    /// shares the same wire shape (ZC_SPIRITS).
    /// </summary>
    public int SpiritCharm { get; set; }
    /// <summary>Element ID of the active spirit charm (rAthena ELE_*).</summary>
    public int SpiritCharmType { get; set; }

    /// <summary>
    /// rAthena Stalker Reproduce — copied skill id / level. 0 = no
    /// active copy. Set via <c>pc_skill_plagiarism</c>; resolver
    /// substitution lands with the Stalker port.
    /// </summary>
    public ushort PlagiarizedSkillId { get; set; }
    public byte PlagiarizedSkillLevel { get; set; }

    /// <summary>
    /// rAthena <c>invincible_timer</c> (pc.cpp:417) — absolute tick
    /// (<see cref="Environment.TickCount64"/>) until which the PC is
    /// invulnerable. 0 = not invincible. Applied automatically on warp
    /// / spawn (rAthena <c>battle.invincible_time</c> default 5000 ms).
    /// </summary>
    public long InvincibleUntilTick { get; set; }

    /// <summary>
    /// rAthena <c>fame</c> on mmo_charstatus — drives smith / alchemist
    /// / taekwon rankings. Mutated by <c>IPlayerFameService.AddFame</c>;
    /// persisted alongside the rest of the character row on autosave.
    /// </summary>
    public int Fame { get; set; }

    /// <summary>rAthena <c>partner_id</c> — married spouse char id, 0 = unmarried.</summary>
    public int PartnerId { get; set; }
    /// <summary>rAthena <c>father</c> — adoptive father char id.</summary>
    public int FatherCharId { get; set; }
    /// <summary>rAthena <c>mother</c> — adoptive mother char id.</summary>
    public int MotherCharId { get; set; }
    /// <summary>rAthena <c>child</c> — adopted baby char id, 0 = no baby.</summary>
    public int ChildCharId { get; set; }

    /// <summary>
    /// Skills learned by this character: skill_id → level. Mirrors
    /// rAthena <c>mmo_charstatus.skill[]</c>. Hydrated from char-server
    /// at session enter; mutated by <c>pc_skillup</c>.
    /// </summary>
    public Dictionary<ushort, byte> LearnedSkills { get; } = new();


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
