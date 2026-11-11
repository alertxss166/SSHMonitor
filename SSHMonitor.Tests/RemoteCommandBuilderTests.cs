using Xunit;

namespace SSHMonitor.Tests;

public sealed class RemoteCommandBuilderTests
{
    [Fact]
    public void QuotePosixEscapesSingleQuote()
    {
        Assert.Equal("'it'\\''s safe'", RemoteCommandBuilder.QuotePosix("it's safe"));
    }

    [Theory]
    [InlineData("$(id)")]
    [InlineData("`id`")]
    [InlineData("; touch /tmp/pwn")]
    [InlineData("&& reboot")]
    public void ProcessCheckKeepsShellSyntaxInsideOneQuotedArgument(string payload)
    {
        var check = new ServiceCheck { Name = "Process", Type = CheckType.Process, Target = payload };

        var command = RemoteCommandBuilder.BuildCheck(check, 0);

        Assert.Contains("pgrep -f -- " + RemoteCommandBuilder.QuotePosix(payload), command, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("nginx; id")]
    [InlineData("nginx$(id)")]
    [InlineData("../nginx")]
    [InlineData("nginx service")]
    public void ServiceCheckRejectsNonIdentifierTargets(string target)
    {
        var check = new ServiceCheck { Name = "Service", Type = CheckType.Service, Target = target };

        Assert.Throws<ArgumentException>(() => RemoteCommandBuilder.BuildCheck(check, 0));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("65536")]
    [InlineData("22; id")]
    [InlineData("$(id)")]
    public void TcpCheckAcceptsOnlyValidPort(string target)
    {
        var check = new ServiceCheck { Name = "TCP", Type = CheckType.Tcp, Target = target };

        Assert.Throws<ArgumentException>(() => RemoteCommandBuilder.BuildCheck(check, 0));
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("--config=/etc/passwd")]
    [InlineData("javascript:alert(1)")]
    public void HttpCheckAcceptsOnlyHttpAndHttps(string target)
    {
        var check = new ServiceCheck { Name = "HTTP", Type = CheckType.Http, Target = target };

        Assert.Throws<ArgumentException>(() => RemoteCommandBuilder.BuildCheck(check, 0));
    }

    [Fact]
    public void HttpCheckEscapesApostropheInUrl()
    {
        const string target = "https://example.test/it's-alive";
        var check = new ServiceCheck { Name = "HTTP", Type = CheckType.Http, Target = target };

        var command = RemoteCommandBuilder.BuildCheck(check, 0);

        Assert.Contains(RemoteCommandBuilder.QuotePosix(target), command, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Bad|name")]
    [InlineData("Bad\nname")]
    [InlineData("Bad\tname")]
    public void CheckNameRejectsProtocolAndControlCharacters(string name)
    {
        var check = new ServiceCheck { Name = name, Type = CheckType.Tcp, Target = "22" };

        Assert.Throws<ArgumentException>(() => RemoteCommandBuilder.BuildCheck(check, 0));
    }

    [Fact]
    public void ValidTcpCheckUsesNumericPortWithoutUserControlledRegex()
    {
        var check = new ServiceCheck { Name = "SSH", Type = CheckType.Tcp, Target = "22" };

        var command = RemoteCommandBuilder.BuildCheck(check, 7);

        Assert.Contains("grep -Eq ':22$'", command, StringComparison.Ordinal);
        Assert.Contains("'7'", command, StringComparison.Ordinal);
    }
}
