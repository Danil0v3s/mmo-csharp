namespace Core.Database.Entities;

/// <summary>
/// A static mob-spawn entry. Ported from rAthena's
/// <c>npc/re/mobs/**/*.txt</c> declarative
/// <c>monster</c> / <c>boss_monster</c> lines:
/// <c>map,x,y,xs,ys &lt;TAB&gt; monster|boss_monster &lt;TAB&gt; name
/// &lt;TAB&gt; mob_id,amount,delay1,delay2[,event[,size[,ai]]]</c>.
///
/// rAthena uses these to populate fields/dungeons with a respawning
/// pool of mobs. The spawn manager periodically rolls and places
/// mobs within the area, respecting the delay timers.
///
/// Pure data — script-bodied mob events (boss-of-the-week with
/// custom OnDeath logic, instance mobs) stay in <c>npc/</c> until the
/// script engine ports.
/// </summary>
public class MobSpawnEntity
{
    public int SpawnId { get; set; }

    public string MapName { get; set; } = string.Empty;
    /// <summary>Center x. 0 means "anywhere on the map" (rAthena random spawn).</summary>
    public short CenterX { get; set; }
    public short CenterY { get; set; }
    /// <summary>Span half-extent. xs=ys=0 means the whole map.</summary>
    public short SpanXs { get; set; }
    public short SpanYs { get; set; }

    /// <summary><c>true</c> when the rAthena directive was <c>boss_monster</c>; the boss flag changes spawn cadence + flag on the entity.</summary>
    public bool IsBoss { get; set; }

    /// <summary>Optional display-name override (rAthena's "name" field). Empty = use mob_db default.</summary>
    public string DisplayName { get; set; } = string.Empty;

    public int MobId { get; set; }
    public int Amount { get; set; }

    /// <summary>Respawn timer base, in ms. rAthena default 5000.</summary>
    public int Delay1 { get; set; }
    /// <summary>Respawn timer variance, in ms.</summary>
    public int Delay2 { get; set; }

    /// <summary>Optional OnEvent label fired on death. Empty when the entry only specifies "<c>,,</c>" trailing.</summary>
    public string EventLabel { get; set; } = string.Empty;

    /// <summary>
    /// Optional size override. 0 = default for the mob_db row; 1 = small,
    /// 2 = large. Only set when the spawn-line explicitly carries the
    /// 6th comma-separated value.
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// Optional AI override. 0 = default per mob_db; values 1..4 set
    /// rAthena's <c>MD_PASSIVE</c>/<c>MD_AGGRESSIVE</c>/etc. overrides.
    /// </summary>
    public int Ai { get; set; }
}
