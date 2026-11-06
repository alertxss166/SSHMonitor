# Architecture

## Goals

SSH Monitor is an agentless desktop client. Its core must remain independent of WPF, SSH.NET, storage format, and any one Linux distribution.

## Boundaries

- **Domain** contains immutable snapshots, profiles, checks, and validation rules.
- **Application** defines use cases (`Connect`, `CollectSnapshot`, profile management) and interfaces implemented by infrastructure.
- **Infrastructure** owns SSH.NET, JSON persistence, remote shell dialect detection, and command execution.
- **Presentation** owns WPF views and view models. Views contain no SSH or persistence logic.

Dependencies point inward. Infrastructure and Presentation may reference Application and Domain; Domain references none of them.

## Configuration versus user data

Operational defaults and limits use validated typed options loaded through .NET configuration. User-created profiles use a repository because .NET configuration is read-only by design. Secrets use ephemeral input or an OS credential provider, never JSON.

## Compatibility strategy

Capabilities are detected once after connecting (available init system, HTTP client, container runtime, and tools). Checks then select a compatible command provider. Unsupported checks return `Unsupported`, distinct from `Down` and transport errors.

## Testing strategy

- Domain validation and formatting: pure unit tests.
- Command builders and parsers: fixture-driven unit tests for systemd and OpenRC hosts.
- SSH adapter: integration tests against disposable Linux containers.
- View models: tests with fake application services and a controllable clock.
