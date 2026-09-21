# Security Policy

## Supported Versions

AI Usage is an early public project. Security fixes are prioritized for the latest published release.

| Version | Support |
| --- | --- |
| Latest release | Supported |
| Older releases | Best effort |

## Reporting a Vulnerability

Do not disclose security vulnerabilities, credentials, authentication material, or private account information in a public issue.

If GitHub private vulnerability reporting is available for this repository, use the repository's **Security** section to report the issue privately.

If private reporting is not available, contact the maintainer through the GitHub profile without including sensitive details and request a private communication channel.

When reporting a vulnerability, include only the minimum information required to reproduce the issue:

- AI Usage version
- Operating system
- Macro Deck version
- Affected provider integration
- Relevant CLI version
- Reproduction steps
- Sanitized logs or error messages

Do not include:

- API keys
- Access tokens
- Passwords
- Authentication cookies
- Full provider configuration files
- Complete `~/.claude.json` contents
- Private account or organization information

## Credential Exposure

AI Usage does not intentionally store provider credentials. It relies on existing authenticated CLI sessions.

If credentials are accidentally exposed while reporting an issue, rotate or revoke them through the corresponding provider as soon as possible.

## Scope

Security reports related to the following areas are especially relevant:

- Unexpected access to provider authentication material
- Credential leakage through logs or diagnostics
- Unsafe external process handling
- Arbitrary command execution
- Package integrity or release artifact tampering
- Unsafe handling of provider CLI output
