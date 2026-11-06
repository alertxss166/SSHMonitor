using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace SSHMonitor;

public sealed class ServerProfile
{
    public string Name { get; set; } = "My server";
    public string Host { get; set; } = "";
    public int Port { get; set; } = 22;
    public string Username { get; set; } = "monitor";
    public string? PrivateKeyPath { get; set; }
    public int RefreshSeconds { get; set; } = 10;
    public string Language { get; set; } = "en";
    public List<ServiceCheck> Checks { get; set; } =
    [
        new() { Name = "SSH", Type = CheckType.Tcp, Target = "22" },
        new() { Name = "Web site", Type = CheckType.Http, Target = "http://127.0.0.1/" }
    ];
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CheckType { Service, Http, Tcp, Process, Docker }

public sealed class ServiceCheck
{
    public string Name { get; set; } = "Service";
    public CheckType Type { get; set; } = CheckType.Service;
    public string Target { get; set; } = "nginx";
}

public sealed record CheckResult(string Name, bool Ok, string Detail);
public sealed record ServerSnapshot(string Hostname, string Os, string Uptime, string Load, long MemoryUsedKb,
    long MemoryTotalKb, string Disk, IReadOnlyList<CheckResult> Checks, IReadOnlyList<string> Connections,
    IReadOnlyList<string> SecurityEvents, DateTimeOffset CollectedAt);

public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";
    [Range(1, 60)] public int ConnectionTimeoutSeconds { get; set; } = 8;
    [Range(1, 120)] public int CommandTimeoutSeconds { get; set; } = 12;
    [Range(1, 60)] public int MinimumRefreshSeconds { get; set; } = 3;
}

public interface IProfileRepository { ServerProfile Load(); void Save(ServerProfile profile); }
public interface IServerMonitor : IDisposable
{
    Task ConnectAsync(ServerProfile profile, string? secret, CancellationToken cancellationToken = default);
    Task<ServerSnapshot> CollectAsync(ServerProfile profile, CancellationToken cancellationToken = default);
}
