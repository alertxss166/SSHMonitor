# Contributing

Thanks for helping make private server monitoring simpler.

## Development

1. Install the .NET 8 SDK on Windows.
2. Run `dotnet restore` and `dotnet build --warnaserror`.
3. Keep domain/application code independent from WPF and SSH.NET.
4. Never add credentials, real server addresses, private keys, or production logs to fixtures.

New check types should define their behavior in the application model and use an infrastructure command provider. Prefer capability detection over distribution-name checks. Treat unsupported tooling separately from a failed health check.

Open an issue before a large UI or storage-format change. Small bug fixes can go directly to a pull request.
