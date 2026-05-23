using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Status;

/// <summary>
/// DBR-1e: in-memory cache of the rAthena status.yml flag matrix.
/// Loaded once at boot from <c>status_db</c> (1001 SCs: DurationLookup +
/// Opt1/Opt2/Opt3) + <c>status_db_flag</c> (4935 typed flag rows with
/// category discriminator).
///
/// <para>
/// Drives the SC engine's Fail/End matrix: when SC X starts, every
/// active SC named in X's Fail list blocks the start; every SC in X's
/// EndOnStart list is ended; EndReturn additionally aborts the start.
/// When SC X ends, every SC in its EndOnEnd list is ended too.
/// </para>
///
/// <para>
/// Case-insensitive on status name lookup so the rAthena-yml form
/// (<c>StoneWait</c>) matches the C# enum form (<c>Stonewait</c>) without
/// per-row aliasing.
/// </para>
/// </summary>
public interface IStatusDbFlagCache
{
    /// <summary>SCs that block this SC from starting if any one is active on the target.</summary>
    IReadOnlySet<StatusType> GetFailScs(StatusType type);
    /// <summary>SCs to end when this SC starts (no abort).</summary>
    IReadOnlySet<StatusType> GetEndOnStart(StatusType type);
    /// <summary>SCs to end when this SC starts — start aborts if any matched.</summary>
    IReadOnlySet<StatusType> GetEndReturn(StatusType type);
    /// <summary>SCs to end when this SC ends.</summary>
    IReadOnlySet<StatusType> GetEndOnEnd(StatusType type);
    /// <summary>Top-line metadata (DurationLookup, Opt1/Opt2/Opt3) for the SC.</summary>
    StatusDbEntity? GetEntry(StatusType type);
    /// <summary>Diagnostics: how many SCs the cache resolved to runtime StatusType.</summary>
    int ResolvedCount { get; }
}

/// <inheritdoc cref="IStatusDbFlagCache"/>
public sealed class StatusDbFlagCache : IStatusDbFlagCache
{
    private static readonly IReadOnlySet<StatusType> _empty =
        new HashSet<StatusType>();

    private readonly Dictionary<StatusType, HashSet<StatusType>> _fail = new();
    private readonly Dictionary<StatusType, HashSet<StatusType>> _endOnStart = new();
    private readonly Dictionary<StatusType, HashSet<StatusType>> _endReturn = new();
    private readonly Dictionary<StatusType, HashSet<StatusType>> _endOnEnd = new();
    private readonly Dictionary<StatusType, StatusDbEntity> _entries = new();
    private readonly ILogger<StatusDbFlagCache> _logger;

    public int ResolvedCount => _entries.Count;

    public StatusDbFlagCache(IServiceScopeFactory scopes, ILogger<StatusDbFlagCache> logger)
    {
        _logger = logger;
        Load(scopes);
    }

    private void Load(IServiceScopeFactory scopes)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IStatusDbRepository>();
            var parents = repo.GetAllAsync().GetAwaiter().GetResult();
            var flags = repo.GetAllFlagsAsync().GetAwaiter().GetResult();

            // Index parents by case-insensitive name → enum resolution.
            // rAthena yml uses StoneWait; C# enum uses Stonewait; ignoreCase
            // bridges the two without per-row aliasing.
            var nameToType = new Dictionary<string, StatusType>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in parents)
            {
                if (!Enum.TryParse<StatusType>(p.StatusName, ignoreCase: true, out var t)) continue;
                nameToType[p.StatusName] = t;
                _entries[t] = p;
            }

            int matrixRows = 0;
            int unresolvedFlagSC = 0;
            int unresolvedSourceSC = 0;
            foreach (var f in flags)
            {
                if (!nameToType.TryGetValue(f.StatusName, out var srcType))
                {
                    unresolvedSourceSC++;
                    continue;
                }
                // For Fail/EndOnStart/EndReturn/EndOnEnd the flag_name is
                // another SC name. For State/CalcFlag/Flag it's a free-form
                // string we don't consume here.
                if (f.Category is "State" or "CalcFlag" or "Flag") continue;
                if (!Enum.TryParse<StatusType>(f.FlagName, ignoreCase: true, out var tgtType))
                {
                    unresolvedFlagSC++;
                    continue;
                }
                var bucket = f.Category switch
                {
                    "Fail" => _fail,
                    "EndOnStart" => _endOnStart,
                    "EndReturn" => _endReturn,
                    "EndOnEnd" => _endOnEnd,
                    "EndOnRestart" => _endOnStart, // entity doc lists this; treat as EndOnStart
                    _ => null,
                };
                if (bucket == null) continue;
                if (!bucket.TryGetValue(srcType, out var set))
                {
                    set = new HashSet<StatusType>();
                    bucket[srcType] = set;
                }
                set.Add(tgtType);
                matrixRows++;
            }

            _logger.LogInformation(
                "status_db loaded: {Parents} SCs resolved (of {YmlSCs}); flag matrix {Matrix} rows " +
                "(fail={Fail}, endOnStart={EoS}, endReturn={ER}, endOnEnd={EoE}); " +
                "unresolved source-SCs={USrc}, unresolved flag-SCs={UFlag}",
                _entries.Count, parents.Count, matrixRows,
                _fail.Values.Sum(s => s.Count),
                _endOnStart.Values.Sum(s => s.Count),
                _endReturn.Values.Sum(s => s.Count),
                _endOnEnd.Values.Sum(s => s.Count),
                unresolvedSourceSC, unresolvedFlagSC);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "status_db load failed; SC fail/end matrix will be empty");
        }
    }

    public IReadOnlySet<StatusType> GetFailScs(StatusType type)
        => _fail.GetValueOrDefault(type) ?? _empty;
    public IReadOnlySet<StatusType> GetEndOnStart(StatusType type)
        => _endOnStart.GetValueOrDefault(type) ?? _empty;
    public IReadOnlySet<StatusType> GetEndReturn(StatusType type)
        => _endReturn.GetValueOrDefault(type) ?? _empty;
    public IReadOnlySet<StatusType> GetEndOnEnd(StatusType type)
        => _endOnEnd.GetValueOrDefault(type) ?? _empty;
    public StatusDbEntity? GetEntry(StatusType type)
        => _entries.GetValueOrDefault(type);
}
