using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class PlayerContext
{
    public Task openStorage(int mode = 0)
        => ScriptStub.CallAsync(Cat, "openStorage", mode);

    public Task openBank() => ScriptStub.CallAsync(Cat, "openBank");
    public Task openMail() => ScriptStub.CallAsync(Cat, "openMail");
    public Task openAuction() => ScriptStub.CallAsync(Cat, "openAuction");
    public Task openRefineUi() => ScriptStub.CallAsync(Cat, "openRefineUi");
    public Task openStylist() => ScriptStub.CallAsync(Cat, "openStylist");
    public Task openDressRoom() => ScriptStub.CallAsync(Cat, "openDressRoom");
    public Task openRoulette() => ScriptStub.CallAsync(Cat, "openRoulette");
    public Task openQuestUi(int? questId = null)
        => ScriptStub.CallAsync(Cat, "openQuestUi", questId);
    public Task openEnchantGrade() => ScriptStub.CallAsync(Cat, "openEnchantGrade");
    public Task openLaphineSynthesis(int? itemId = null)
        => ScriptStub.CallAsync(Cat, "openLaphineSynthesis", itemId);
    public Task openLaphineUpgrade() => ScriptStub.CallAsync(Cat, "openLaphineUpgrade");
    public Task openItemEnchant(int luaIndex)
        => ScriptStub.CallAsync(Cat, "openItemEnchant", luaIndex);
    public Task openItemReform(int? itemId = null)
        => ScriptStub.CallAsync(Cat, "openItemReform", itemId);
    public Task specialPopup(int popupId)
        => ScriptStub.CallAsync(Cat, "specialPopup", popupId);
    public Task openTips(int tipId)
        => ScriptStub.CallAsync(Cat, "openTips", tipId);
    public Task readBook(int bookId, int page = 0)
        => ScriptStub.CallAsync(Cat, "readBook", bookId, page);
}
