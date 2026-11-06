# Changelog

All notable changes to this project are documented here. The format follows Keep a Changelog and the project uses Semantic Versioning.

## [0.1.0] - 2026-11-06

### Added

- Password and private-key SSH authentication.
- Agentless Linux metrics: uptime, memory, load average, and root disk usage.
- Configurable service, HTTP, TCP, process, and Docker checks.
- Active TCP connection view with `ss` and `netstat` support.
- SSH authentication and firewall event view.
- English, Russian, and Simplified Chinese interface.
- Dark Windows title bars and dark controls.
- Local profile storage without passwords.
- GitHub Actions CI, Dependabot, security policy, and contribution guide.

### Security

- Updated SSH.NET to 2026.0.0.
- Shell-escaped user-configurable check targets.
- Repository checked for embedded credentials, private keys, tokens, and real server addresses.

