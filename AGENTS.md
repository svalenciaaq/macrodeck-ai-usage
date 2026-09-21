# Agent guidance

Keep this file aligned with the actual repository. When the architecture, public variables, provider integrations, build requirements, or release process change, update this file as part of the same change.

## Project

AI Usage is a Macro Deck 3 plugin for monitoring usage limits from:

- OpenAI Codex
- Anthropic Claude Code
- Google Gemini through Antigravity

Current platform:

- Linux x64
- .NET 10
- Macro Deck 3

The plugin exposes variables only. It currently exposes no Macro Deck actions.

## Public API

The plugin intentionally exposes exactly six variables:

```text
ai_usage_codex_5h_card
ai_usage_codex_week_card
ai_usage_claude_5h_card
ai_usage_claude_week_card
ai_usage_gemini_5h_card
ai_usage_gemini_week_card
```

Local Macro Deck IDs:

```text
codex-5h-card
codex-week-card
claude-5h-card
claude-week-card
gemini-5h-card
gemini-week-card
```

Treat these IDs as public API.

Do not rename or remove them without considering existing Macro Deck button bindings.

Do not restore the previous fine grained variables unless that is an intentional product change.

## Card format

Each variable returns one multiline text value.

Example:

```text
91
█████████░
4d 8h
```

The lines represent:

1. Remaining usage percentage
2. Remaining usage bar
3. Time until reset

## Architecture

```text
src/AI.Usage/
  Program.cs
      Plugin host entry point.

  manifest.json
      Plugin identity, version, runtime and compatibility.

  macrodeck-build.json
      Package build configuration.

  PluginIntegration.cs
      Macro Deck lifecycle and public variables.

  UsageCoordinator.cs
      Background refresh coordination and current snapshots.

  SnapshotCache.cs
      Snapshot caching and retry behavior.

  CodexUsageService.cs
      Codex app server integration and parser.

  ClaudeUsageService.cs
      Claude Code usage refresh, local cache reader and parser.

  GeminiUsageService.cs
      Antigravity usage integration and parser.

  Properties/AssemblyInfo.cs
      Internal visibility for the test assembly.

tests/AI.Usage.Tests/
  PluginIntegrationTests.cs
      Tests the Macro Deck public integration surface.

  UsageParsingTests.cs
      Tests provider parsing without launching provider CLIs.
```

## Refresh architecture

`PluginIntegration` owns one `UsageCoordinator`.

The coordinator obtains the initial provider snapshots and then refreshes providers in the background.

Macro Deck variable reads must use the in memory snapshots.

Do not launch Codex, Claude Code, or Antigravity directly from individual variable reads.

When a provider temporarily fails, preserve the last valid snapshot.

Log the beginning of a provider failure episode and its recovery without generating repetitive warnings every refresh cycle.

## Codex

Codex usage is retrieved through:

```text
codex app-server --listen stdio://
```

Communication uses JSON RPC.

Expected rate limit windows:

```text
300 minutes
10080 minutes
```

External Codex processes must always be cleaned up.

## Claude Code

Claude usage is read from:

```text
~/.claude.json
```

Relevant cached data:

```text
cachedUsageUtilization
```

The plugin periodically invokes:

```text
claude -p /usage
```

to refresh Claude Code usage information.

External Claude processes must always be cleaned up.

## Gemini

Gemini usage is retrieved through:

```text
agy --print "/usage" --output-format json
```

Only the following usage group is used:

```text
Gemini Models
```

Automatic Antigravity CLI updates are disabled for this invocation.

External Antigravity processes must always be cleaned up.

## External process rules

Use `ProcessStartInfo`.

Use:

```text
UseShellExecute = false
RedirectStandardOutput = true
RedirectStandardError = true
```

Use `ArgumentList` rather than constructing shell command strings.

Consume redirected output streams.

On timeout or cancellation, terminate remaining process trees when necessary:

```csharp
process.Kill(entireProcessTree: true);
```

Never pass provider credentials through command line arguments.

## Parsing

Keep provider parsing separate from external process execution whenever practical.

Parser methods used by tests may remain internal.

Tests must use representative local JSON samples and must not depend on installed or authenticated provider CLIs.

Malformed or incomplete provider responses must not result in invented usage values.

## Security

Never commit:

```text
API keys
access tokens
passwords
authentication cookies
provider credentials
personal account identifiers
.env files
manual backup files
build artifacts
```

The plugin relies on existing authenticated CLI sessions.

Do not log credentials or authentication configuration.

## Tests

Run:

```bash
dotnet test
```

Tests must remain deterministic and independent of live provider sessions.

Current coverage includes:

- Six public variables
- Variable IDs and names
- Variable types
- Duplicate protection
- No Macro Deck actions
- Codex parsing
- Claude parsing
- Gemini parsing
- Invalid and incomplete provider data

When a provider response format changes, update both the parser and its tests.

## Validation

Before release changes are committed:

```bash
dotnet clean
dotnet build
dotnet test
git diff --check
```

Expected result:

```text
0 build errors
0 build warnings
all tests passing
no whitespace errors
```

## Packaging

Build the plugin with:

```bash
macrodeck-plugin build \
  --source src/AI.Usage \
  --rid linux-x64 \
  --output ./artifacts \
  --force
```

Do not commit `artifacts/`.

Validate and inspect the exact generated `.macroDeckPlugin` before publishing it.

## Manifest

`src/AI.Usage/manifest.json` is the authoritative plugin identity.

Current identity:

```text
id: com.svalencia.ai-usage
name: AI Usage
publisher: SVA
license: MIT
platform: linux-x64
runtime: .NET 10
Macro Deck compatibility: >=3.0.0-0
```

Keep the manifest version synchronized with the release being packaged.

## Documentation

`README.md` is public user documentation.

`AGENTS.md` contains repository guidance for development agents.

Do not leave template instructions or documentation describing code that no longer exists.

## Change discipline

Prefer small changes.

Preserve the six variable public surface unless an intentional breaking change is being made.

Do not suppress analyzer warnings without documenting why.

Do not add telemetry, credential storage, network services, or additional external dependencies without an explicit product decision.

After changing provider integrations, run the complete test suite and perform a real Macro Deck smoke test before release.
