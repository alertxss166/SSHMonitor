# Security policy

Report vulnerabilities privately through GitHub Security Advisories. Do not open a public issue for a suspected vulnerability.

## Security model

- Passwords are held in memory for the connection attempt and are never written to the profile.
- A dedicated least-privilege server account is strongly recommended.
- Check targets are shell-escaped before being placed in remote commands.
- Profiles must not contain keys, passwords, tokens, or other secrets.
- Before v1.0, use only with servers you trust. Host-key pinning is a release blocker for v1.0.
