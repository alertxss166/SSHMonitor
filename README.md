# SSH Monitor

> **English** — SSH Monitor is a private, agentless monitoring tool for your personal Linux server. It connects through the SSH port you already use, displays system health, checks your services, and shows active connections and security events. Metrics stay between your Windows computer and your server: no cloud account, public dashboard, inbound monitoring port, or permanent server agent is required.
>
> **Русский** — SSH Monitor — это приватный инструмент без серверного агента для мониторинга личного Linux-сервера. Он подключается через уже используемый SSH-порт, показывает состояние системы и сервисов, активные соединения и события безопасности. Данные передаются только между вашим Windows-компьютером и сервером: облачная учётная запись, публичная панель, дополнительный входящий порт и постоянно работающий агент не нужны.
>
> **中文** — SSH Monitor 是一款用于个人 Linux 服务器的私有、无代理监控工具。它通过您现有的 SSH 端口连接，显示系统状态、服务检查、活动连接和安全事件。监控数据仅在您的 Windows 电脑与服务器之间传输：无需云账户、公共仪表板、额外开放端口或常驻服务器代理。

## What it monitors / Что отслеживается / 监控内容

- Uptime, memory usage, load average, operating system, and root-disk usage.
- systemd and OpenRC services.
- HTTP/HTTPS endpoints, listening TCP ports, processes, and Docker containers.
- Active TCP connections through either `ss` or `netstat`.
- Recent SSH authentication and firewall `DROP`/`REJECT` events when the monitoring account can read them.
- Automatic refresh with an interval configured per profile.

The desktop interface is available in English, Russian, and Simplified Chinese.

## Download and run

### For ordinary users

1. Open the repository's **Releases** page.
2. Download `SSHMonitor-vX.Y.Z-win-x64.zip`.
3. Extract the archive.
4. Run `SSHMonitor.exe`.

The release is self-contained and includes the required .NET runtime and native WPF libraries. A user needs only:

- 64-bit Windows 10 or Windows 11;
- network access to the server's SSH port;
- a Linux monitoring account and its password or OpenSSH private key.

The .NET SDK, Visual Studio, PowerShell modules, Node.js, Docker Desktop, and a server-side SSH Monitor installation are **not required** for users.

Windows SmartScreen may warn about early unsigned builds. Verify the SHA-256 checksum shown in the GitHub Release before running the file. Code signing is planned for a later release.

### First connection

Enter the server address, SSH port, username, and password, then select **Connect**. Passwords are used only for the current connection and are never written to the profile.

Open **Checks & settings** to add a check:

```text
Name:   Nginx
Type:   Service
Target: nginx
```

For an Alpine web server, a useful second check is:

```text
Name:   Website
Type:   Http
Target: http://127.0.0.1/
```

## Dedicated monitoring account

Do not use `root`. Create a dedicated account with only the permissions required to read metrics:

```sh
sudo useradd --create-home --shell /bin/sh monitor
sudo passwd monitor
```

On Alpine Linux, the equivalent account can be created with:

```sh
doas adduser monitor
```

Base `/proc`, disk, HTTP, TCP, and process checks normally need no elevated privileges. Access to authentication/firewall logs depends on the distribution's file permissions. Grant narrowly scoped group access or exact read-only commands; never grant unrestricted passwordless `sudo`/`doas` merely for monitoring.

## Security and privacy model

- Monitoring travels inside an encrypted SSH connection.
- No monitoring service listens on an additional server port.
- No telemetry, analytics, cloud relay, or public dashboard is used.
- Passwords are kept only for the connection attempt and are never saved.
- Profiles contain the host, port, username, check definitions, refresh interval, language, and optional local key path.
- Private-key contents are never copied into the profile or repository.
- Active connections and security events are kept in memory and are not written to application logs.
- Check targets are shell-escaped before commands are executed remotely.
- A dedicated least-privilege server account is strongly recommended.

Host-key pinning and `known_hosts` management remain release blockers for a security-stable v1.0. Until then, use SSH Monitor only with servers and networks you trust. See [SECURITY.md](SECURITY.md).

## Architecture

SSH Monitor is a Windows WPF application targeting .NET 8. Its design follows dependency inversion and separates responsibilities:

```text
Presentation — WPF windows, controls, localization and view state
       ↓
Application — connection and snapshot use cases, interfaces and validation
       ↓
Domain — profiles, check definitions, results and snapshots
       ↑
Infrastructure — SSH.NET transport, Linux command adapter and JSON profiles
```

Important architectural properties:

- **Agentless pull model:** the Windows client initiates every collection cycle over SSH. Nothing is deployed as a daemon on the Linux host.
- **Transport abstraction:** presentation code depends on `IServerMonitor`, not directly on SSH.NET.
- **Storage abstraction:** profiles are accessed through `IProfileRepository`; passwords are not part of the profile model.
- **Dependency injection:** the .NET Generic Host composes services and manages their lifetime.
- **Typed configuration:** operational timeouts are bound to `MonitoringOptions` and validated at startup.
- **Capability fallback:** TCP and connection checks use `ss` when available and fall back to `netstat`; service checks support systemd and OpenRC.
- **Explicit compatibility:** unreadable security logs produce an informational empty state rather than being persisted or uploaded.
- **Bounded collection:** remote commands have timeouts and connection/log output is capped to prevent unbounded UI and memory growth.
- **Localization:** user-facing labels use the selected EN/RU/中文 dictionary; protocol tokens remain culture-neutral.
- **Release isolation:** build outputs, package caches, profiles, keys, and local configuration are excluded from Git.

More detail is available in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Updates and versioning

- The running SSH Monitor version is displayed beside the application name.
- Releases use Semantic Versioning and are documented in [CHANGELOG.md](CHANGELOG.md).
- `global.json` pins the supported .NET SDK and allows only compatible patch roll-forward.
- Dependabot checks NuGet and GitHub Actions dependencies weekly.
- Scheduled CI reports outdated packages and fails on vulnerable dependencies or build warnings.
- SSH.NET is pinned to an audited explicit version rather than an unbounded range.
- A tag such as `v0.1.0` builds a self-contained Windows x64 archive through GitHub Actions.

An in-app GitHub Release notification will be enabled after the permanent public repository URL is known; the update feed will be configurable rather than tied to a developer's personal address.

## Building from source

Developers need Windows and the .NET SDK version declared in `global.json`:

```powershell
dotnet restore
dotnet build --configuration Release --warnaserror
dotnet run --configuration Release
```

Create the same self-contained single-file release used by GitHub Actions:

```powershell
dotnet publish --configuration Release --runtime win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true
```

## Supported server environments

The base collector expects a Linux system with `/proc`, POSIX `sh`, `awk`, `grep`, `sed`, `df`, and standard core utilities. Designed environments include:

- Alpine Linux with OpenRC;
- Debian and Ubuntu with systemd;
- Fedora and Rocky Linux with systemd;
- other `/proc`-based distributions with compatible tools.

Optional features require the corresponding server tool: `curl` or `wget` for HTTP checks, Docker CLI for Docker checks, and readable system logs for SSH/firewall events.

## Roadmap

- Host-key fingerprint confirmation and `known_hosts` management.
- Multiple servers and dashboard groups.
- Local SQLite history and charts.
- Windows, Telegram, Discord, and generic webhook alerts.
- Import/export without secrets.
- Signed installer and portable releases.
- Configurable in-app GitHub Release notifications.

## Contributing

Issues and pull requests are welcome. Never include passwords, keys, public addresses, or unredacted production logs. See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

MIT

