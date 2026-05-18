namespace Map.Server.Scripting.Dialog;

// JS has Math.random etc., but rAthena scripts lean on `rand` so we expose
// a thin wrapper too.
public sealed partial class DialogContext
{
    public int rand(int max) => Random.Shared.Next(max);
    public int randRange(int min, int max) => Random.Shared.Next(min, max + 1);
}
