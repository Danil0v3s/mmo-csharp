using Core.Server.Packets.Out.ZC;
using Map.Server.Achievement;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Pet.PetOps;
using Map.Server.Quest;
using Map.Server.Services;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Mob;

/// <summary>
/// Default <see cref="IMobDeathObserver"/>. Mirrors rAthena <c>mob_dead</c> steps 4–7
/// (mob.cpp): MVP block → quest objectives → achievement objectives → pet catch. Quest and
/// achievement progress is mutated on the live <see cref="PlayerEntity"/> log and rides the existing
/// FEATURE-02 save fan-out; pet catch creates an egg char-side; MVP awards exp + one MVP drop and a
/// world announce.
/// </summary>
public sealed class MobDeathObserver : IMobDeathObserver
{
    private readonly IEntityRegistry _entities;
    private readonly IQuestService _quest;
    private readonly IAchievementService _achievement;
    private readonly IPetOpsService _pet;
    private readonly IExpService? _exp;
    private readonly IItemDropService? _itemDrops;
    private readonly IItemCatalog? _items;
    private readonly IPlayerMapService? _players;
    private readonly ISessionManagerAccessor? _sessions;
    private readonly ILogger<MobDeathObserver> _logger;

    public MobDeathObserver(
        IEntityRegistry entities,
        IQuestService quest,
        IAchievementService achievement,
        IPetOpsService pet,
        ILogger<MobDeathObserver> logger,
        IExpService? exp = null,
        IItemDropService? itemDrops = null,
        IItemCatalog? items = null,
        IPlayerMapService? players = null,
        ISessionManagerAccessor? sessions = null)
    {
        _entities = entities;
        _quest = quest;
        _achievement = achievement;
        _pet = pet;
        _logger = logger;
        _exp = exp;
        _itemDrops = itemDrops;
        _items = items;
        _players = players;
        _sessions = sessions;
    }

    public void OnMobDead(MobEntity mob, PlayerEntity? killer, IReadOnlyList<MobDmgList.DmgEntry> dmgLog)
    {
        if (mob.DbEntry == null) return;

        // 1. Contributor set — distinct PCs that dealt damage, plus the last-hitter.
        var contributors = ResolveContributors(dmgLog, killer);

        // 2. MVP block (rAthena: mexp>0 or mvp drops) — credit the top-damage PC.
        if (mob.DbEntry.MvpExp > 0 || mob.DbEntry.MvpDrops.Count > 0)
            AwardMvp(mob, dmgLog, killer);

        // 3. Quest + 4. achievement objectives — per contributing PC. FEATURE-21: pass the mob's
        // full matching context (id/aegis/level/race/size/element) so any-mob filter objectives
        // (race/size/element/level/location) resolve, not just aegis-specific ones.
        var db = mob.DbEntry;
        var questMob = new Map.Server.Quest.QuestMobContext(
            MobId: db.Id, Aegis: db.AegisName ?? string.Empty, Level: db.Level,
            Race: db.Race, Size: db.Size, Element: db.Element);
        var achievementMatch = _achievement.MobExists(mob.ClassId);
        foreach (var pc in contributors)
        {
            _quest.UpdateMobObjective(pc, questMob);
            if (achievementMatch)
                _achievement.UpdateObjective(pc, (byte)AchievementGroup.Battle, 0, mob.ClassId);
        }

        // 5. Pet catch — only the catcher (rAthena keys off the killer's catch_target_class).
        if (killer != null && killer.PetCatchTargetClass == mob.ClassId)
            _pet.CatchProcessEnd(killer, mob.ClassId);
    }

    /// <summary>Distinct live PCs in the damage log, plus the killer (deduped).</summary>
    private List<PlayerEntity> ResolveContributors(IReadOnlyList<MobDmgList.DmgEntry> dmgLog, PlayerEntity? killer)
    {
        var seen = new HashSet<EntityId>();
        var result = new List<PlayerEntity>();
        foreach (var e in dmgLog)
        {
            if (_entities.Get(e.AttackerId) is PlayerEntity pc && pc.Hp > 0 && seen.Add(pc.Id))
                result.Add(pc);
        }
        if (killer != null && seen.Add(killer.Id))
            result.Add(killer);
        return result;
    }

    private void AwardMvp(MobEntity mob, IReadOnlyList<MobDmgList.DmgEntry> dmgLog, PlayerEntity? killer)
    {
        // rAthena mvp_sd = the top cumulative-damage attacker; fall back to the last-hitter.
        PlayerEntity? mvp = null;
        var top = mob.DmgList.TopDamageAttacker();
        if (top is { } id && _entities.Get(id) is PlayerEntity tp) mvp = tp;
        mvp ??= killer;
        if (mvp == null) return;

        if (mob.DbEntry!.MvpExp > 0)
            _exp?.GainExp(mvp, mob.DbEntry.MvpExp, 0, mob.Level);

        // One MVP drop: roll each slot, drop the first that passes (rAthena mob_dead MVP loop).
        foreach (var drop in mob.DbEntry.MvpDrops)
        {
            if (drop.Rate <= 0) continue;
            if (_rng.Next(10_000) >= drop.Rate) continue;
            var row = _items?.GetByAegisName(drop.Item);
            if (row == null) continue;
            _itemDrops?.DropOnFloor(mob.MapId, mob.X, mob.Y, itemId: (int)row.Id, amount: 1,
                subX: 0, subY: 0, identified: true,
                ownerCharId: mvp.CharacterId, ownerPartyId: mvp.PartyId, ownerGuildId: 0, isMvpDrop: true);
            break;
        }

        // World announce (rAthena clif_mvp_effect + BC_DEFAULT broadcast). ZC_BROADCAST2 is the real
        // client-visible part; the dedicated ZC_MVP item/exp effect packets are PACKET-* scope
        // (see MVP-EFFECT-PACKET follow-up).
        Announce($"[ {mvp.Name} ] has hunted the MVP monster [ {mob.Name} ].");
        _logger.LogInformation("MVP {Mob} killed by {Pc} (mvpExp={Exp})", mob.Name, mvp.Name, mob.DbEntry.MvpExp);
    }

    private void Announce(string text)
    {
        if (_players == null || _sessions == null) return;
        var packet = new ZC_BROADCAST2
        {
            FontColor = 0x00FF00, // MVP announce — green (rAthena uses a styled broadcast)
            FontType = 0,
            FontSize = 12,
            FontAlign = 0,
            FontY = 0,
            Message = text,
        };
        foreach (var p in _players.GetAllPlayers())
            _sessions.GetByEntityId(p.Id)?.EnqueuePacket(packet);
    }

    // Deterministic MVP-drop rolls are seeded off the shared RNG; the death path is single-threaded
    // (game loop), so a process-wide instance is safe.
    private readonly Random _rng = Random.Shared;
}
