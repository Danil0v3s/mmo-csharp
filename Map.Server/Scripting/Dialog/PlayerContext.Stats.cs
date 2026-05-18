using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Microsoft.ClearScript;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Scripting.Dialog;

public sealed partial class PlayerContext
{
    public int str => (int)(_session.CharacterData?.Str ?? 1);
    public int agi => (int)(_session.CharacterData?.Agi ?? 1);
    public int vit => (int)(_session.CharacterData?.Vit ?? 1);
    [ScriptMember("int")] public int intStat => (int)(_session.CharacterData?.IntStat ?? 1);
    public int dex => (int)(_session.CharacterData?.Dex ?? 1);
    public int luk => (int)(_session.CharacterData?.Luk ?? 1);
    public int statusPoint => (int)(_session.CharacterData?.StatusPoint ?? 0);
    public int skillPoint => (int)(_session.CharacterData?.SkillPoint ?? 0);
}
