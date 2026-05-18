namespace Map.Server.Scripting.Dialog;

// All player sub-surfaces live here. Each is a tiny class exposing one
// rAthena script subsystem (quests, achievements, storage, cart, mail,
// pet, homunculus, mercenary). Every method is a ScriptStub — the
// surface is final, the internals come later. Grouped in one file so
// the player API shape is easy to skim in one place.

public sealed class PlayerQuestSurface
{
    private const string Cat = "player.quest";

    public Task add(int questId)                      => ScriptStub.CallAsync(Cat, "add", questId);
    public Task complete(int questId)                 => ScriptStub.CallAsync(Cat, "complete", questId);
    public Task erase(int questId)                    => ScriptStub.CallAsync(Cat, "erase", questId);
    public Task change(int fromId, int toId)          => ScriptStub.CallAsync(Cat, "change", fromId, toId);
    /// <summary>state: 0 = inactive, 1 = active, 2 = complete. Mode: "any" | "playtime" | "hunting".</summary>
    public int check(int questId, string mode = "any")=> ScriptStub.Call(Cat, "check", 0, questId, mode);
    public bool isBegin(int questId)                  => ScriptStub.Call(Cat, "isBegin", false, questId);
    public Task showEvent(int icon, int markColor = 0)=> ScriptStub.CallAsync(Cat, "showEvent", icon, markColor);
    public Task refreshInfo()                         => ScriptStub.CallAsync(Cat, "refreshInfo");
    public Task showInfo(int icon, int markColor = 0, string? condition = null)
        => ScriptStub.CallAsync(Cat, "showInfo", icon, markColor, condition);
}

public sealed class PlayerAchievementSurface
{
    private const string Cat = "player.achievement";

    public Task add(int achievementId)                       => ScriptStub.CallAsync(Cat, "add", achievementId);
    public Task remove(int achievementId)                    => ScriptStub.CallAsync(Cat, "remove", achievementId);
    public Task complete(int achievementId)                  => ScriptStub.CallAsync(Cat, "complete", achievementId);
    public bool exists(int achievementId)                    => ScriptStub.Call(Cat, "exists", false, achievementId);
    public int info(int achievementId, int type)             => ScriptStub.Call(Cat, "info", 0, achievementId, type);
    public Task update(int achievementId, int type, int value)
        => ScriptStub.CallAsync(Cat, "update", achievementId, type, value);
}

public sealed class PlayerStorageSurface
{
    private const string Cat = "player.storage";

    public Task open(int mode = 0)                              => ScriptStub.CallAsync(Cat, "open", mode);
    public Task openExtra(int storageId, int mode = 0)          => ScriptStub.CallAsync(Cat, "openExtra", storageId, mode);
    public int countItem(int itemId, object? opts = null)       => ScriptStub.Call(Cat, "countItem", 0, itemId, opts);
    public Task delItem(int itemId, int amount, object? opts = null)
        => ScriptStub.CallAsync(Cat, "delItem", itemId, amount, opts);

    public Task openGuildStorage()                              => ScriptStub.CallAsync(Cat, "openGuildStorage");
    public int countGuildItem(int itemId, object? opts = null)  => ScriptStub.Call(Cat, "countGuildItem", 0, itemId, opts);
    public Task delGuildItem(int itemId, int amount, object? opts = null)
        => ScriptStub.CallAsync(Cat, "delGuildItem", itemId, amount, opts);
    public object[] guildLog()                                  => ScriptStub.Call<object[]>(Cat, "guildLog", Array.Empty<object>());
}

public sealed class PlayerCartSurface
{
    private const string Cat = "player.cart";

    public bool isEnabled()                                     => ScriptStub.Call(Cat, "isEnabled", false);
    public int countItem(int itemId, object? opts = null)       => ScriptStub.Call(Cat, "countItem", 0, itemId, opts);
    public Task delItem(int itemId, int amount, object? opts = null)
        => ScriptStub.CallAsync(Cat, "delItem", itemId, amount, opts);
}

public sealed class PlayerMailSurface
{
    private const string Cat = "player.mail";

    public Task open() => ScriptStub.CallAsync(Cat, "open");
}

public sealed class PlayerPetSurface
{
    private const string Cat = "player.pet";

    public Task catchPet(int itemId, int flag = 0)              => ScriptStub.CallAsync(Cat, "catchPet", itemId, flag);
    public Task makePet(int petId)                              => ScriptStub.CallAsync(Cat, "makePet", petId);
    public Task birthPet()                                      => ScriptStub.CallAsync(Cat, "birthPet");
    public Task openIncubator()                                 => ScriptStub.CallAsync(Cat, "openIncubator");
    public object? info(int type)                               => ScriptStub.Call<object?>(Cat, "info", null, type);
    public Task skillBonus(int bonusType, int value, int durationMs, int delayMs)
        => ScriptStub.CallAsync(Cat, "skillBonus", bonusType, value, durationMs, delayMs);
    public Task skillSupport(int skillId, int skillLv, int delayMs, int hpPct, int spPct)
        => ScriptStub.CallAsync(Cat, "skillSupport", skillId, skillLv, delayMs, hpPct, spPct);
    public Task skillAttack(int skillId, int skillLv, int rate, int bonusRate)
        => ScriptStub.CallAsync(Cat, "skillAttack", skillId, skillLv, rate, bonusRate);
    public Task skillAttack2(int skillId, int damage, int attacks, int rate, int bonusRate)
        => ScriptStub.CallAsync(Cat, "skillAttack2", skillId, damage, attacks, rate, bonusRate);
    public Task recovery(int statusType, int delayMs)
        => ScriptStub.CallAsync(Cat, "recovery", statusType, delayMs);
    public Task loot(int maxItems)                              => ScriptStub.CallAsync(Cat, "loot", maxItems);
}

public sealed class PlayerHomSurface
{
    private const string Cat = "player.hom";

    public bool exists()                              => ScriptStub.Call(Cat, "exists", false);
    public bool isCalled()                            => ScriptStub.Call(Cat, "isCalled", false);
    public object? info(int type)                     => ScriptStub.Call<object?>(Cat, "info", null, type);
    public Task evolve()                              => ScriptStub.CallAsync(Cat, "evolve");
    public Task morph()                               => ScriptStub.CallAsync(Cat, "morph");
    public Task mutate(int? id = null)                => ScriptStub.CallAsync(Cat, "mutate", id);
    public Task shuffle()                             => ScriptStub.CallAsync(Cat, "shuffle");
    public Task addIntimacy(int amount)               => ScriptStub.CallAsync(Cat, "addIntimacy", amount);
}

public sealed class PlayerMercSurface
{
    private const string Cat = "player.merc";

    public Task create(int classId, int contractTimeSec)
        => ScriptStub.CallAsync(Cat, "create", classId, contractTimeSec);
    public Task delete(int reply = 0)                                 => ScriptStub.CallAsync(Cat, "delete", reply);
    public Task heal(int hp, int sp = 0)                              => ScriptStub.CallAsync(Cat, "heal", hp, sp);
    public Task scStart(int type, int durationMs, int val1)           => ScriptStub.CallAsync(Cat, "scStart", type, durationMs, val1);
    public int getCalls(int guildType)                                => ScriptStub.Call(Cat, "getCalls", 0, guildType);
    public Task setCalls(int guildType, int value)                    => ScriptStub.CallAsync(Cat, "setCalls", guildType, value);
    public int getFaith(int guildType)                                => ScriptStub.Call(Cat, "getFaith", 0, guildType);
    public Task setFaith(int guildType, int value)                    => ScriptStub.CallAsync(Cat, "setFaith", guildType, value);
    public object? info(int type)                                     => ScriptStub.Call<object?>(Cat, "info", null, type);
    public object? elementalInfo(int type)                            => ScriptStub.Call<object?>(Cat, "elementalInfo", null, type);
}
