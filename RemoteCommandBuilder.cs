using System.Globalization;

namespace SSHMonitor;

public static class RemoteCommandBuilder
{
    private const int MaxNameLength = 128;
    private const int MaxTargetLength = 2048;

    public static string BuildCheck(ServiceCheck check, int index)
    {
        ArgumentNullException.ThrowIfNull(check);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        ValidateDisplayName(check.Name);
        var name = QuotePosix(check.Name);
        var test = check.Type switch
        {
            CheckType.Service => BuildServiceCheck(check.Target),
            CheckType.Http => BuildHttpCheck(check.Target),
            CheckType.Tcp => BuildTcpCheck(check.Target),
            CheckType.Process => BuildProcessCheck(check.Target),
            CheckType.Docker => BuildDockerCheck(check.Target),
            _ => throw new ArgumentOutOfRangeException(nameof(check), check.Type, "Unsupported check type.")
        };
        var id = index.ToString(CultureInfo.InvariantCulture);
        return $"if {test}; then printf '@@CHECK|%s|UP|%s\\n' '{id}' {name}; else printf '@@CHECK|%s|DOWN|%s\\n' '{id}' {name}; fi";
    }

    public static string QuotePosix(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return "'" + value.Replace("'", "'\\''", StringComparison.Ordinal) + "'";
    }

    private static string BuildServiceCheck(string target)
    {
        ValidateIdentifier(target, "service", allowSlash: false);
        var quoted = QuotePosix(target);
        return $"(systemctl is-active --quiet {quoted} 2>/dev/null || rc-service {quoted} status >/dev/null 2>&1)";
    }

    private static string BuildHttpCheck(string target)
    {
        ValidateTarget(target);
        if (!Uri.TryCreate(target, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("HTTP checks require an absolute http:// or https:// URL.", nameof(target));
        var quoted = QuotePosix(target);
        return $"(curl -fsS --max-time 4 --url {quoted} >/dev/null 2>&1 || wget -q -T 4 -O /dev/null {quoted})";
    }

    private static string BuildTcpCheck(string target)
    {
        if (!int.TryParse(target, NumberStyles.None, CultureInfo.InvariantCulture, out var port) || port is < 1 or > 65535)
            throw new ArgumentException("TCP checks require a port from 1 to 65535.", nameof(target));
        return $"((if command -v ss >/dev/null 2>&1; then ss -lnt; elif command -v netstat >/dev/null 2>&1; then netstat -lnt; else exit 1; fi) 2>/dev/null | awk '{{print $4}}' | grep -Eq ':{port}$')";
    }

    private static string BuildProcessCheck(string target)
    {
        ValidateTarget(target);
        return $"pgrep -f -- {QuotePosix(target)} >/dev/null 2>&1";
    }

    private static string BuildDockerCheck(string target)
    {
        ValidateIdentifier(target, "container", allowSlash: true);
        return $"[ \"$(docker inspect -f '{{{{.State.Running}}}}' {QuotePosix(target)} 2>/dev/null)\" = true ]";
    }

    private static void ValidateDisplayName(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxNameLength)
            throw new ArgumentException($"Check name must contain 1 to {MaxNameLength} characters.", nameof(value));
        if (value.Contains('|', StringComparison.Ordinal) || value.Any(char.IsControl))
            throw new ArgumentException("Check name cannot contain protocol separators or control characters.", nameof(value));
    }

    private static void ValidateTarget(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxTargetLength)
            throw new ArgumentException($"Check target must contain 1 to {MaxTargetLength} characters.", nameof(value));
        if (value.Any(char.IsControl))
            throw new ArgumentException("Check target cannot contain control characters.", nameof(value));
    }

    private static void ValidateIdentifier(string value, string kind, bool allowSlash)
    {
        ValidateTarget(value);
        if (value.Any(c => !IsIdentifierCharacter(c, allowSlash)))
            throw new ArgumentException($"Invalid {kind} identifier.", nameof(value));
    }

    private static bool IsIdentifierCharacter(char value, bool allowSlash) =>
        char.IsAsciiLetterOrDigit(value) || value is '.' or '_' or '-' or '@' or ':' || (allowSlash && value == '/');
}
