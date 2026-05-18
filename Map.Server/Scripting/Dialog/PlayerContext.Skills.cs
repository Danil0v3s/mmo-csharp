using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class PlayerContext
{
    public int skillLv(int skillId)
        => ScriptStub.Call(Cat, "skillLv", 0, skillId);

    public Task addSkill(int skillId, int level, object? opts = null)
        => ScriptStub.CallAsync(Cat, "addSkill", skillId, level, opts);

    public Task itemSkill(int skillId, int level, bool keepRequirement = false)
        => ScriptStub.CallAsync(Cat, "itemSkill", skillId, level, keepRequirement);

    public object[] getSkillList()
        => ScriptStub.Call<object[]>(Cat, "getSkillList", Array.Empty<object>());

    public int skillPointCount()
        => ScriptStub.Call(Cat, "skillPointCount", skillPoint);

    public bool basicSkillCheck()
        => ScriptStub.Call(Cat, "basicSkillCheck", true);
}
