using Microsoft.Extensions.Logging;

namespace Core.Server.Monitoring;

public readonly record struct MonitoredEndpoint(int Id, uint Ip, int Port, string Name);

public sealed class EndpointLivenessMonitor
{
    private readonly ILogger _logger;
    private readonly string _scope;
    private readonly TimeSpan _probeInterval;
    private readonly TimeSpan _connectTimeout;
    private readonly int _failureThreshold;
    private readonly Dictionary<int, int> _failureCounts = new();
    private DateTime _nextProbeUtc = DateTime.MinValue;

    public EndpointLivenessMonitor(
        ILogger logger,
        string scope,
        TimeSpan probeInterval,
        TimeSpan connectTimeout,
        int failureThreshold)
    {
        _logger = logger;
        _scope = scope;
        _probeInterval = probeInterval;
        _connectTimeout = connectTimeout;
        _failureThreshold = Math.Max(failureThreshold, 1);
    }

    public async Task<IReadOnlyList<MonitoredEndpoint>> ProbeDueEndpointsAsync(
        IEnumerable<MonitoredEndpoint> endpoints,
        CancellationToken cancellationToken = default)
    {
        if (DateTime.UtcNow < _nextProbeUtc)
        {
            return Array.Empty<MonitoredEndpoint>();
        }

        _nextProbeUtc = DateTime.UtcNow.Add(_probeInterval);

        var endpointList = endpoints.ToList();
        var activeIds = endpointList.Select(endpoint => endpoint.Id).ToHashSet();
        var staleFailureKeys = _failureCounts.Keys.Where(id => !activeIds.Contains(id)).ToList();
        foreach (var staleId in staleFailureKeys)
        {
            _failureCounts.Remove(staleId);
        }

        var unreachable = new List<MonitoredEndpoint>();
        foreach (var endpoint in endpointList)
        {
            var isReachable = await EndpointProbe.TryConnectAsync(
                endpoint.Ip,
                endpoint.Port,
                _connectTimeout,
                cancellationToken);

            if (isReachable)
            {
                _failureCounts.Remove(endpoint.Id);
                continue;
            }

            var failures = _failureCounts.TryGetValue(endpoint.Id, out var currentFailures)
                ? currentFailures + 1
                : 1;
            _failureCounts[endpoint.Id] = failures;

            _logger.LogWarning(
                "{Scope} liveness probe failed for server id {ServerId} ({ServerName}) {Failures}/{Threshold} at {Endpoint}",
                _scope,
                endpoint.Id,
                endpoint.Name,
                failures,
                _failureThreshold,
                EndpointProbe.FormatEndpoint(endpoint.Ip, endpoint.Port));

            if (failures < _failureThreshold)
            {
                continue;
            }

            unreachable.Add(endpoint);
            _failureCounts.Remove(endpoint.Id);
        }

        return unreachable;
    }
}
