# Contributing to AI Usage

Contributions are welcome. AI Usage is intentionally small and keeps a narrow public integration surface, so changes should preserve predictable behavior and deterministic tests.

## Development Requirements

- .NET SDK 10
- Macro Deck Plugin CLI
- Linux x64 for the currently supported package target

Restore dependencies:

```bash
dotnet restore AI.Usage.slnx
```

Build with warnings treated as errors:

```bash
dotnet build AI.Usage.slnx \
  --configuration Release \
  --no-restore \
  --warnaserror
```

Run the deterministic test suite:

```bash
dotnet test AI.Usage.slnx \
  --configuration Release \
  --no-build
```

## Public Plugin Contract

The current public surface contains exactly six text variables:

- `ai_usage_codex_5h_card`
- `ai_usage_codex_week_card`
- `ai_usage_claude_5h_card`
- `ai_usage_claude_week_card`
- `ai_usage_gemini_5h_card`
- `ai_usage_gemini_week_card`

The plugin currently exposes no Macro Deck actions.

Changes to this surface should be treated as intentional compatibility changes and documented clearly.

## Provider Integration Rules

Provider integrations currently cover:

- OpenAI Codex
- Anthropic Claude Code
- Google Gemini through Antigravity

Provider CLI processes must run only during background refresh work. Macro Deck variable reads should use in-memory snapshots and must not launch provider processes directly.

External processes must be cleaned up on completion, timeout, and cancellation paths.

Last valid snapshots should be preserved when a provider temporarily fails.

## Tests

Tests must remain deterministic and must not require authenticated Codex, Claude Code, or Antigravity sessions.

Parsing logic should be tested with representative provider output and invalid or incomplete responses.

Before opening a pull request, verify:

```bash
dotnet restore AI.Usage.slnx
dotnet build AI.Usage.slnx --configuration Release --no-restore --warnaserror
dotnet test AI.Usage.slnx --configuration Release --no-build
git diff --check
```

## Pull Requests

Keep pull requests focused on one logical change.

A pull request should include:

- A concise description of the problem
- The reason for the proposed change
- Tests for behavior changes
- Documentation updates when public behavior changes
- Sanitized diagnostic output when relevant

Do not include secrets or private provider configuration.

## Repository Guidance

`AGENTS.md` contains repository-specific guidance for AI-assisted development. Changes made with an AI coding agent should follow the same build, test, security, and public-contract requirements as manually authored changes.

## Security

For security vulnerabilities, follow [SECURITY.md](SECURITY.md) instead of opening a public issue containing sensitive information.
