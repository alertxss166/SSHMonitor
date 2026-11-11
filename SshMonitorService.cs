using Renci.SshNet;
using System.Text;
using Microsoft.Extensions.Options;

namespace SSHMonitor;

public sealed class SshServerMonitor : IServerMonitor
{
    private SshClient? _client;
    private readonly MonitoringOptions _options;
    public SshServerMonitor(IOptions<MonitoringOptions> options) => _options = options.Value;

    public Task ConnectAsync(ServerProfile profile, string? secret, CancellationToken cancellationToken = default) => Task.Run(() =>
    {
        Dispose();
        AuthenticationMethod auth;
        if (!string.IsNullOrWhiteSpace(profile.PrivateKeyPath))
            auth = new PrivateKeyAuthenticationMethod(profile.Username, new PrivateKeyFile(profile.PrivateKeyPath, secret));
        else if (!string.IsNullOrEmpty(secret))
            auth = new PasswordAuthenticationMethod(profile.Username, secret);
        else throw new InvalidOperationException("Enter a password or select a private key.");

        _client = new SshClient(new ConnectionInfo(profile.Host, profile.Port, profile.Username, auth)
        { Timeout = TimeSpan.FromSeconds(_options.ConnectionTimeoutSeconds) });
        _client.KeepAliveInterval = TimeSpan.FromSeconds(20);
        _client.Connect();
    }, cancellationToken);

    public Task<ServerSnapshot> CollectAsync(ServerProfile profile, CancellationToken cancellationToken = default) => Task.Run(() =>
    {
        if (_client is not { IsConnected: true }) throw new InvalidOperationException("Not connected.");
        var checks = string.Join("\n", profile.Checks.Select(RemoteCommandBuilder.BuildCheck));
        var script = $$"""
            hostname=$(hostname 2>/dev/null || echo unknown)
            os=$(sed -n 's/^PRETTY_NAME=//p' /etc/os-release 2>/dev/null | tr -d '"' | head -1)
            uptime_s=$(cut -d. -f1 /proc/uptime 2>/dev/null || echo 0)
            load=$(cut -d' ' -f1-3 /proc/loadavg 2>/dev/null || echo '? ? ?')
            mem=$(awk '/MemTotal:/ {t=$2} /MemAvailable:/ {a=$2} END {print t-a "," t}' /proc/meminfo 2>/dev/null)
            disk=$(df -Pk / 2>/dev/null | awk 'NR==2 {print $3 "," $2 "," $5}')
            printf '@@META|%s|%s|%s|%s|%s|%s\n' "$hostname" "$os" "$uptime_s" "$load" "$mem" "$disk"
            {{checks}}
            if command -v ss >/dev/null 2>&1; then
              ss -Htn 2>/dev/null | awk '$1 != "LISTEN" {print "@@CONN|" $0}' | head -30
            elif command -v netstat >/dev/null 2>&1; then
              netstat -tn 2>/dev/null | awk 'NR > 2 && $6 != "TIME_WAIT" {print "@@CONN|" $4 "  ->  " $5 "  [" $6 "]"}' | head -30
            fi
            security_log=""
            for candidate in /var/log/auth.log /var/log/secure /var/log/messages; do
              if [ -r "$candidate" ]; then security_log="$candidate"; break; fi
            done
            if [ -n "$security_log" ]; then
              grep -iE 'sshd.*(Accepted|Failed|Invalid|authentication|preauth)|nft.*(drop|reject)|firewall.*(drop|reject)' "$security_log" 2>/dev/null | tail -30 | sed 's/^/@@SEC|/'
            elif command -v journalctl >/dev/null 2>&1; then
              journalctl --no-pager -n 100 -u ssh -u sshd 2>/dev/null | grep -iE 'Accepted|Failed|Invalid|authentication|preauth' | tail -30 | sed 's/^/@@SEC|/'
            fi
            """;
        var output = Run(script);
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var meta = lines.FirstOrDefault(x => x.StartsWith("@@META|", StringComparison.Ordinal))?.Split('|')
            ?? throw new InvalidOperationException("The server returned no metrics. Is it Linux with /proc mounted?");
        var mem = meta.ElementAtOrDefault(5)?.Split(',') ?? ["0", "0"];
        var results = lines.Where(x => x.StartsWith("@@CHECK|", StringComparison.Ordinal)).Select(ParseCheck).ToList();
        var connections = lines.Where(x => x.StartsWith("@@CONN|", StringComparison.Ordinal)).Select(x => x[7..]).ToList();
        var securityEvents = lines.Where(x => x.StartsWith("@@SEC|", StringComparison.Ordinal)).Select(x => x[6..]).ToList();
        return new ServerSnapshot(meta.ElementAtOrDefault(1) ?? "?", meta.ElementAtOrDefault(2) ?? "Linux",
            FormatUptime(meta.ElementAtOrDefault(3)), meta.ElementAtOrDefault(4) ?? "?", ParseLong(mem.ElementAtOrDefault(0)),
            ParseLong(mem.ElementAtOrDefault(1)), meta.ElementAtOrDefault(6) ?? "?", results, connections, securityEvents, DateTimeOffset.Now);
    }, cancellationToken);

    private string Run(string command)
    {
        using var cmd = _client!.CreateCommand(command);
        cmd.CommandTimeout = TimeSpan.FromSeconds(_options.CommandTimeoutSeconds);
        var result = cmd.Execute();
        if (cmd.ExitStatus != 0 && string.IsNullOrWhiteSpace(result)) throw new InvalidOperationException(cmd.Error);
        return result;
    }

    private static CheckResult ParseCheck(string line)
    {
        var p = line.Split('|');
        return new(p.ElementAtOrDefault(3) ?? "Check", p.ElementAtOrDefault(2) == "UP", p.ElementAtOrDefault(2) ?? "UNKNOWN");
    }
    private static long ParseLong(string? value) => long.TryParse(value, out var n) ? n : 0;
    private static string FormatUptime(string? value) { var t = TimeSpan.FromSeconds(ParseLong(value)); return $"{(int)t.TotalDays}d {t.Hours}h {t.Minutes}m"; }
    public void Dispose() { _client?.Dispose(); _client = null; }
}
