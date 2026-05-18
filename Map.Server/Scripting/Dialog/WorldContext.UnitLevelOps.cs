namespace Map.Server.Scripting.Dialog;

public sealed partial class WorldContext
{
    public Task unitWalk(long gid, int x, int y, string? onArriveEvent = null)
        => ScriptStub.CallAsync(Cat, "unitWalk", gid, x, y, onArriveEvent);
    public Task unitWalkToTarget(long gid, long targetGid, string? onArriveEvent = null)
        => ScriptStub.CallAsync(Cat, "unitWalkToTarget", gid, targetGid, onArriveEvent);
    public Task unitAttack(long gid, long targetGid, int actionType = 0)
        => ScriptStub.CallAsync(Cat, "unitAttack", gid, targetGid, actionType);
    public Task unitKill(long gid) => ScriptStub.CallAsync(Cat, "unitKill", gid);
    public Task unitWarp(long gid, string mapName, int x, int y)
        => ScriptStub.CallAsync(Cat, "unitWarp", gid, mapName, x, y);
    public Task unitTalk(long gid, string text, int flag = 0)
        => ScriptStub.CallAsync(Cat, "unitTalk", gid, text, flag);
    public Task unitSkillUseId(long gid, int skillId, int skillLv, object? opts = null)
        => ScriptStub.CallAsync(Cat, "unitSkillUseId", gid, skillId, skillLv, opts);
    public Task unitSkillUsePos(long gid, int skillId, int skillLv, int x, int y, object? opts = null)
        => ScriptStub.CallAsync(Cat, "unitSkillUsePos", gid, skillId, skillLv, x, y, opts);
    public Task unitStopAttack(long gid) => ScriptStub.CallAsync(Cat, "unitStopAttack", gid);
    public Task unitStopWalk(long gid, int flag = 0)
        => ScriptStub.CallAsync(Cat, "unitStopWalk", gid, flag);
    public bool unitExists(long gid) => ScriptStub.Call(Cat, "unitExists", false, gid);
    public int getUnitType(long gid) => ScriptStub.Call(Cat, "getUnitType", 0, gid);
    public string getUnitName(long gid) => ScriptStub.Call(Cat, "getUnitName", string.Empty, gid);
    public Task setUnitName(long gid, string name) => ScriptStub.CallAsync(Cat, "setUnitName", gid, name);
    public string getUnitTitle(long gid) => ScriptStub.Call(Cat, "getUnitTitle", string.Empty, gid);
    public Task setUnitTitle(long gid, string title) => ScriptStub.CallAsync(Cat, "setUnitTitle", gid, title);
    public object? getUnitData(long gid) => ScriptStub.Call<object?>(Cat, "getUnitData", null, gid);
    public Task setUnitData(long gid, int parameter, object value)
        => ScriptStub.CallAsync(Cat, "setUnitData", gid, parameter, value);

    public long[] getUnits(int type) => ScriptStub.Call(Cat, "getUnits", Array.Empty<long>(), type);
    public long[] getMapUnits(int type, string mapName)
        => ScriptStub.Call(Cat, "getMapUnits", Array.Empty<long>(), type, mapName);
    public long[] getAreaUnits(int type, string mapName, int x1, int y1, int x2, int y2)
        => ScriptStub.Call(Cat, "getAreaUnits", Array.Empty<long>(), type, mapName, x1, y1, x2, y2);
}
