using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class PlayerContext
{
    public int id => _entity.CharacterId;
    public int charId => _entity.CharacterId;
    public int accountId => _entity.AccountId;
    public string name => _entity.Name;
    public int sex => _session.Sex;
    public int classId => (int)(_session.CharacterData?.ClassId ?? 0);
    public int baseLevel => (int)(_session.CharacterData?.BaseLevel ?? 1);
    public int jobLevel => (int)(_session.CharacterData?.JobLevel ?? 1);
    public int groupId => (int)_session.GroupId;
    public int gmLevel => (int)_session.GroupId;
    public int partyId => ScriptStub.Call(Cat, "partyId", 0);
    public int guildId => ScriptStub.Call(Cat, "guildId", 0);
    public int weight => ScriptStub.Call(Cat, "weight", 0);
    public int maxWeight => ScriptStub.Call(Cat, "maxWeight", 20000);
    public string mapName => _session.CharacterData?.MapName ?? string.Empty;
    public int x => _entity.X;
    public int y => _entity.Y;
    public int dir => _entity.Dir;
}
