namespace Core.Database.Entities;

/// <summary>
/// A static warp portal. Ported from rAthena's <c>npc/re/warps/*.txt</c>
/// declarative <c>warp</c> / <c>warp2</c> lines — the format is
/// <c>srcmap,x,y,dir &lt;TAB&gt; warp|warp2 &lt;TAB&gt; name
/// &lt;TAB&gt; xs,ys,destmap,destx,desty</c>.
///
/// Scripted warps (<c>WARPNPC</c> with a body that branches on quest
/// state, party size, etc.) are deliberately out of scope here — they
/// live in <c>npc/...</c> alongside other scripted NPCs and require
/// the script engine to evaluate.
/// </summary>
public class WarpEntity
{
    /// <summary>Auto-increment surrogate key.</summary>
    public int WarpId { get; set; }

    /// <summary>Source map name (rAthena map_index name, no <c>.gat</c> suffix).</summary>
    public string SrcMap { get; set; } = string.Empty;
    public short SrcX { get; set; }
    public short SrcY { get; set; }
    /// <summary>Facing direction at the source cell. 0 = north, clockwise. Rarely non-zero for warps.</summary>
    public byte SrcDir { get; set; }

    /// <summary>
    /// Type of warp. rAthena's <c>warp</c> (most common) vs <c>warp2</c>
    /// (used by warps that fire on the cell-walk-onto event with slightly
    /// different sprite). Map server treats both identically for spawn /
    /// teleport semantics.
    /// </summary>
    public string WarpType { get; set; } = "warp";

    /// <summary>
    /// Script name. rAthena uses these for cross-referencing (the script
    /// engine can disable a warp by name). Unique per <c>(srcmap, srcx, srcy)</c>.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Trigger-area span half-extent. The trigger covers (x-xs..x+xs, y-ys..y+ys).</summary>
    public short SpanXs { get; set; }
    public short SpanYs { get; set; }

    public string DstMap { get; set; } = string.Empty;
    public short DstX { get; set; }
    public short DstY { get; set; }
}
