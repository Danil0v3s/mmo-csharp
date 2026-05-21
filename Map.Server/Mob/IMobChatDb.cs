namespace Map.Server.Mob;

/// <summary>
/// Port of rAthena's <c>mob_chat_db</c> (mob.cpp:86, mob.hpp).
/// One row per chat msg id, used by <c>mob_chat_display_message</c>
/// (mob.cpp:4205) — looked up when a mob_skill_db row sets
/// <c>msg_id</c> (rAthena MobSkillEntry.MsgId / our
/// <see cref="MobSkillEntry.ChatId"/>).
///
/// <para>The full rAthena loader reads <c>db/mob_chat_db.yml</c> on
/// boot. Our first slice ships the interface + an in-memory impl;
/// callers (tests, future YAML loader) register rows via
/// <see cref="Add"/>. Once the YAML loader lands it slots into the
/// same shape.</para>
/// </summary>
public interface IMobChatDb
{
    /// <summary>Look up by msg id. Null if not registered.</summary>
    MobChatRow? Find(int msgId);

    /// <summary>Register / overwrite a row.</summary>
    void Add(MobChatRow row);

    /// <summary>Count of registered rows.</summary>
    int Count { get; }
}

/// <summary>
/// One row of mob_chat_db. Matches rAthena <c>s_mob_chat</c>:
/// msg id, RGB color, message text.
/// </summary>
public sealed record MobChatRow(int MsgId, uint ColorRgb, string Message);
