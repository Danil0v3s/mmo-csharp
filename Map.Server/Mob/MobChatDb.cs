using System.Collections.Concurrent;

namespace Map.Server.Mob;

/// <summary>
/// In-memory <see cref="IMobChatDb"/>. The real YAML loader (planned
/// in a follow-up wave) will populate this on boot from
/// <c>db/mob_chat_db.yml</c>; until then tests + scripts register
/// rows directly. The concurrent dict lets the boot path / hot-reload
/// callers add rows without locking.
/// </summary>
public sealed class MobChatDb : IMobChatDb
{
    private readonly ConcurrentDictionary<int, MobChatRow> _rows = new();

    public MobChatRow? Find(int msgId)
        => _rows.TryGetValue(msgId, out var row) ? row : null;

    public void Add(MobChatRow row) => _rows[row.MsgId] = row;

    public int Count => _rows.Count;
}
