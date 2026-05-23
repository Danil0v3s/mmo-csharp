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

    /// <summary>
    /// rAthena <c>trait_point</c> on mmo_charstatus — renewal POW/STA/
    /// WIS/SPL/CON/CRT allocation pool, separate from <see cref="StatusPoints"/>.
    /// Mutated by <c>pc_traitstatusup</c>.
    /// </summary>
    public int TraitPoints { get; set; }

    /// <summary>rAthena <c>cashPoints</c> — cash shop currency (paid).</summary>
    public int CashPoints { get; set; }
    /// <summary>rAthena <c>kafraPoints</c> — kafra-shop currency (earned).</summary>
    public int KafraPoints { get; set; }

    /// <summary>Party id, 0 = solo. Sourced from char-server at session enter.</summary>
    public int PartyId { get; set; }

    /// <summary>Guild id, 0 = none. Sourced from char-server at session enter.</summary>
    public int GuildId { get; set; }

    /// <summary>Clan id, 0 = none. Sourced from char-server / clan_db.yml.</summary>
    public int ClanId { get; set; }

    /// <summary>Player group id (gm level). Mirrors session's GroupId; surfaced here for scripts.</summary>
    public int GroupId { get; set; }

    /// <summary>
    /// Accumulated equipment-bonus bundle. Mirrors rAthena's
    /// <c>indexed_bonus</c> struct on <c>map_session_data</c>
    /// (status.hpp:1980). Recomputed by
    /// <see cref="Inventory.EquipBonusAggregator"/> on every equip /
    /// unequip / break / strip / refine. Read by
    /// <see cref="Combat.IBattleCardService.CalcCardFix"/> + the
    /// cast-time / drain / autospell pipelines.
    /// </summary>
    public Inventory.EquipBonusBundle EquipBonuses { get; set; } = new();

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
    /// rAthena <c>memo_point[3]</c> — Warp Portal memo slots.
    /// Each is (mapName, x, y). Empty mapName = empty slot.
    /// </summary>
    public (string MapName, short X, short Y)[] MemoPoints { get; } = new (string, short, short)[3];

    /// <summary>
    /// Skills learned by this character: skill_id → level. Mirrors
    /// rAthena <c>mmo_charstatus.skill[].lv</c>. Hydrated from
    /// char-server at session enter; mutated by <c>pc_skillup</c>.
    /// </summary>
    public Dictionary<ushort, byte> LearnedSkills { get; } = new();

    /// <summary>
    /// Per-skill flag — rAthena <c>mmo_charstatus.skill[].flag</c>
    /// (SKILL_FLAG_*). Default <see cref="Skills.SkillFlag.Permanent"/>
    /// for legacy entries; CleanSkillTree only drops PermanentGranted.
    /// </summary>
    public Dictionary<ushort, Skills.SkillFlag> SkillFlags { get; } = new();

    /// <summary>
    /// T5.1a — incoming-hit log mirroring the rAthena PC side of
    /// <c>unit_counttargeted</c> (unit.cpp). Populated by
    /// <see cref="Combat.IDamageService.ApplyDamage"/> on every hit
    /// the PC takes from a known source. Read by
    /// <see cref="Mob.Conditions.MasterAttackedCondition"/> — when a
    /// homunculus / mercenary's master is a PC, the slave's
    /// MSC_MASTERATTACKED rule queries
    /// <see cref="Mob.MobDmgList.DistinctAttackerCount"/>.
    /// Same shape as <see cref="MobEntity.DmgList"/> so future
    /// distinct-attacker queries (PVP last-hit, BG MVP, fame
    /// attribution) read through one canonical surface.
    /// </summary>
    public Map.Server.Mob.MobDmgList AttackerLog { get; } = new();

    /// <summary>
    /// T7.1 — per-PC active quest log. Mirror of rAthena
    /// <c>sd-&gt;quest_log[]</c> (pc.hpp). Hydrated at session enter
    /// from the char-side <c>QuestLoadAsync</c> RPC (via
    /// <see cref="Map.Server.Quest.IQuestService.Hydrate"/>),
    /// snapshotted at save time by
    /// <see cref="Map.Server.Quest.IQuestService.SnapshotFor"/>.
    /// </summary>
    public List<Map.Server.Quest.QuestEntry> QuestLog { get; } = new();

    /// <summary>
    /// T7.1 — per-PC achievement progress log. Mirror of rAthena
    /// <c>sd-&gt;achievement_data.achievements</c>
    /// (achievement.hpp). Same hydrate/snapshot pattern as
    /// <see cref="QuestLog"/>.
    /// </summary>
    public List<Map.Server.Achievement.AchievementEntry> AchievementLog { get; } = new();

    // ----- GM atcommand flags (AT-C wave) -----
    // Most of these are toggle/state fields that atcommands set on the
    // PC. They live here so the wire layer can read them without a
    // service lookup and so future status_calc paths see them as
    // part of PlayerEntity state.

    /// <summary>rAthena <c>sd-&gt;state.showexp</c> — toggle EXP-gain popup.</summary>
    public bool ShowExp { get; set; }
    /// <summary>rAthena <c>sd-&gt;state.showzeny</c> — toggle zeny-gain popup.</summary>
    public bool ShowZeny { get; set; }
    /// <summary>rAthena <c>sd-&gt;state.showdelay</c> — show skill-delay debug.</summary>
    public bool ShowDelay { get; set; }
    /// <summary>Server-side toggle for the <c>@showmobs</c> persistent mob marker overlay.</summary>
    public bool ShowMobs { get; set; }
    /// <summary>rAthena <c>sd-&gt;state.noask</c> — auto-reject invites.</summary>
    public bool NoAsk { get; set; }
    /// <summary>rAthena <c>sd-&gt;state.noks</c> — kill-steal protection.</summary>
    public bool NoKs { get; set; }
    /// <summary>rAthena <c>sd-&gt;status.manner</c> — chat mute level (minutes remaining ×−1; 0 = unmuted).</summary>
    public short Manner { get; set; }
    /// <summary>Target char-id for <c>@follow</c>. 0 = not following.</summary>
    public int FollowTargetCharId { get; set; }
    /// <summary>rAthena <c>sd-&gt;status.body</c> — alt-costume body style id.</summary>
    public short BodyStyle { get; set; }
    /// <summary>rAthena <c>sd-&gt;langtype</c> — message language index.</summary>
    public byte LangType { get; set; }
    /// <summary>rAthena <c>sd-&gt;disguise</c> — overriding sprite class id (0 = normal).</summary>
    public int DisguiseClassId { get; set; }
    /// <summary>rAthena <c>sd-&gt;state.size</c> — visual size (0=small, 1=normal, 2=large).</summary>
    public byte ViewSize { get; set; } = 1;
    /// <summary>rAthena <c>sd-&gt;status.ap</c> — Abyss Points (renewal aux resource).</summary>
    public int Ap { get; set; }
    /// <summary>rAthena <c>sd-&gt;status.max_ap</c> — Abyss Points cap.</summary>
    public int MaxAp { get; set; } = 100;
    /// <summary>rAthena Taekwon <c>sd-&gt;hate_mob[3]</c> — three hate-target mob class ids; -1 = unset.</summary>
    public int[] HateMobs { get; } = new int[] { -1, -1, -1 };
    /// <summary>rAthena Taekwon <c>sd-&gt;feel_map[3]</c> — three feeling map ids; 0 = unset.</summary>
    public (string MapName, int Index)[] FeelMaps { get; } = new (string, int)[3];
    /// <summary>rAthena <c>sd-&gt;state.guildspy</c> — observe guild id (0 = off).</summary>
    public int GuildSpyId { get; set; }
    /// <summary>rAthena <c>sd-&gt;state.partyspy</c> — observe party id (0 = off).</summary>
    public int PartySpyId { get; set; }
    /// <summary>rAthena <c>sd-&gt;state.autoloot</c> — auto-pick all dropped items.</summary>
    public byte AutoLootRate { get; set; }
    /// <summary>rAthena <c>sd-&gt;state.autolootid</c> — per-itemid whitelist.</summary>
    public HashSet<int> AutoLootIds { get; } = new();
    /// <summary>Idle-flag for the <c>@idle</c> command — purely informational.</summary>
    public bool Idle { get; set; }
    /// <summary>Fake name overlay for <c>@fakename</c>; empty = use real name.</summary>
    public string FakeName { get; set; } = string.Empty;

    // ----- Per-PC working drafts (AT-D2 wave) -----
    /// <summary>
    /// rAthena <c>mail.msg</c> — per-PC mail draft. Attached items + zeny
    /// are held here between <c>mail_setattachment</c> /
    /// <c>mail_removeitem</c> and the actual <c>mail_send</c>; the draft
    /// is wiped on send or on <c>mail_clear</c>. Index: inventory slot.
    /// </summary>
    public Dictionary<int, int> MailDraftItems { get; } = new();
    /// <summary>rAthena <c>mail.msg.zeny</c>.</summary>
    public long MailDraftZeny { get; set; }
    /// <summary>rAthena <c>mail.opened</c> — mail UI open flag.</summary>
    public bool MailOpened { get; set; }

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
