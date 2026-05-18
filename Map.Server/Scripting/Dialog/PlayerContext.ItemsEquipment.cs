using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class PlayerContext
{
    public int getEquipId(int slot)
        => ScriptStub.Call(Cat, "getEquipId", 0, slot);

    public string getEquipName(int slot)
        => ScriptStub.Call(Cat, "getEquipName", string.Empty, slot);

    public long getEquipUniqueId(int slot)
        => ScriptStub.Call<long>(Cat, "getEquipUniqueId", 0, slot);

    public int getEquipRefine(int slot)
        => ScriptStub.Call(Cat, "getEquipRefine", 0, slot);

    public int getEquipWeaponLv(int slot = -1)
        => ScriptStub.Call(Cat, "getEquipWeaponLv", 0, slot);

    public int getEquipArmorLv(int slot = -1)
        => ScriptStub.Call(Cat, "getEquipArmorLv", 0, slot);

    public int getEquipCardCount(int slot)
        => ScriptStub.Call(Cat, "getEquipCardCount", 0, slot);

    public int getEquipCardId(int slot, int cardSlot)
        => ScriptStub.Call(Cat, "getEquipCardId", 0, slot, cardSlot);

    public int getEnchantGrade(int slot = -1)
        => ScriptStub.Call(Cat, "getEnchantGrade", 0, slot);

    public bool isEquipped(int slot)
        => ScriptStub.Call(Cat, "isEquipped", false, slot);

    public bool isEquipEnableRef(int slot)
        => ScriptStub.Call(Cat, "isEquipEnableRef", false, slot);

    public int getItemPos(int slot)
        => ScriptStub.Call(Cat, "getItemPos", 0, slot);

    public Task equip(int itemId)
        => ScriptStub.CallAsync(Cat, "equip", itemId);

    public Task autoEquip(int itemId, bool enable)
        => ScriptStub.CallAsync(Cat, "autoEquip", itemId, enable);

    public Task unequip(int slot)
        => ScriptStub.CallAsync(Cat, "unequip", slot);

    public Task delEquip(int slot)
        => ScriptStub.CallAsync(Cat, "delEquip", slot);

    public Task breakEquip(int slot)
        => ScriptStub.CallAsync(Cat, "breakEquip", slot);

    public Task successRefine(int slot, int count = 1)
        => ScriptStub.CallAsync(Cat, "successRefine", slot, count);

    public Task failRefine(int slot)
        => ScriptStub.CallAsync(Cat, "failRefine", slot);

    public Task downRefine(int slot, int count = 1)
        => ScriptStub.CallAsync(Cat, "downRefine", slot, count);

    public Task repair(int brokenIndex)
        => ScriptStub.CallAsync(Cat, "repair", brokenIndex);

    public Task repairAll()
        => ScriptStub.CallAsync(Cat, "repairAll");

    public Task removeCards(int slot, bool success, int type = 0)
        => ScriptStub.CallAsync(Cat, "removeCards", slot, success, type);

    public int getBrokenId(int number)
        => ScriptStub.Call(Cat, "getBrokenId", 0, number);
}
