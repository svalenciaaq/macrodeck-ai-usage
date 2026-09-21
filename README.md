# AI Usage for Macro Deck

AI Usage is a Macro Deck 3 plugin for monitoring usage limits from:

- OpenAI Codex
- Anthropic Claude Code
- Google Gemini via Antigravity

It exposes compact usage cards that can be placed directly on Macro Deck buttons.

## Features

- 5 hour usage window
- Weekly usage window
- Remaining usage percentage
- Visual remaining usage bar
- Time until reset
- Automatic background refresh
- Cached values when a provider is temporarily unavailable
- Six consolidated variables designed for Macro Deck buttons
- No provider API keys stored by the plugin

## Available Variables

The plugin exposes exactly six variables:

- `ai_usage_codex_5h_card`
- `ai_usage_codex_week_card`
- `ai_usage_claude_5h_card`
- `ai_usage_claude_week_card`
- `ai_usage_gemini_5h_card`
- `ai_usage_gemini_week_card`

Each variable contains a complete card value.

Example:

```text
91
█████████░
4d 8h
```

The three lines represent:

1. Remaining usage percentage
2. Visual remaining usage bar
3. Time until reset

The percentage is intentionally returned without the `%` symbol so the value remains compact on Macro Deck buttons.

## Requirements

- Macro Deck 3
- .NET 10 runtime
- Codex CLI for Codex monitoring
- Claude Code for Claude monitoring
- Antigravity CLI for Gemini monitoring

The corresponding CLI tools must already be installed and authenticated for the providers you want to use.

## Data Sources

### OpenAI Codex

Codex usage is retrieved through the Codex app server.

The plugin starts:

```text
codex app-server --listen stdio://
```

and uses the app server JSON RPC interface to retrieve account rate limits.

The plugin uses the available:

- 5 hour window
- Weekly window

### Anthropic Claude Code

Claude usage is read from the local Claude Code usage cache stored in:

```text
~/.claude.json
```

The relevant information is read from:

```text
cachedUsageUtilization
```

The plugin periodically invokes:

```text
claude -p /usage
```

to refresh Claude Code's local usage data before reading the latest values.

### Google Gemini

Gemini usage is retrieved through Antigravity CLI:

```text
agy --print "/usage" --output-format json
```

Only the:

```text
Gemini Models
```

usage group is used.

Automatic Antigravity CLI updates are disabled during these requests.

## Installation

1. Download the latest `.macroDeckPlugin` file from GitHub Releases.
2. Open Macro Deck.
3. Install the plugin.
4. Open the Variables section.
5. Add the AI Usage variables to your buttons.

The provider CLI tools must already be installed and authenticated on the machine running Macro Deck.

## Example Button Layout

Each button can use a single card variable.

### Codex

5 hour usage:

```text
CODEX 5 HOURS

{{ vars.ai_usage_codex_5h_card }}
```

Weekly usage:

```text
CODEX WEEK

{{ vars.ai_usage_codex_week_card }}
```

### Claude Code

5 hour usage:

```text
CLAUDE 5 HOURS

{{ vars.ai_usage_claude_5h_card }}
```

Weekly usage:

```text
CLAUDE WEEK

{{ vars.ai_usage_claude_week_card }}
```

### Gemini

5 hour usage:

```text
GEMINI 5 HOURS

{{ vars.ai_usage_gemini_5h_card }}
```

Weekly usage:

```text
GEMINI WEEK

{{ vars.ai_usage_gemini_week_card }}
```

## Refresh Behavior

Usage information is refreshed automatically in the background.

Macro Deck variable reads use the latest in memory snapshot instead of launching provider CLI processes for every variable request.

This keeps variable reads lightweight and avoids unnecessary process execution.

### Cached Values

When a provider temporarily fails or becomes unavailable, AI Usage keeps the last valid snapshot instead of immediately clearing the displayed value.

This helps avoid temporary `n/v` states caused by short CLI failures, provider delays, or transient connection issues.

### Codex

Codex rate limits are refreshed periodically through the Codex app server.

### Claude Code

Claude usage is read from the local Claude usage cache.

The plugin periodically asks Claude Code to refresh that cache before reading the latest values.

### Gemini

Gemini usage is refreshed periodically through Antigravity CLI.

## Process Management

AI Usage starts provider CLI processes only when usage information needs to be refreshed.

External processes are cleaned up after use and are terminated when necessary during timeout or cancellation scenarios.

The plugin does not execute provider CLIs directly from individual Macro Deck variable reads.

## Privacy and Credentials

AI Usage does not require provider API keys to be stored inside the plugin.

It relies on the existing authenticated sessions of the installed CLI tools.

The plugin does not intentionally store:

- API keys
- Provider authentication tokens
- Passwords
- Account credentials

Authentication remains managed by Codex CLI, Claude Code, and Antigravity.

AI Usage does not include telemetry.

## Building from Source

### Requirements

- .NET SDK 10
- Macro Deck Plugin CLI

Clone the repository and build the project:

```bash
dotnet build
```

Run the tests:

```bash
dotnet test
```

Build the Macro Deck plugin package:

```bash
macrodeck-plugin build \
  --source src/AI.Usage \
  --rid linux-x64 \
  --output ./artifacts \
  --force
```

The generated `.macroDeckPlugin` file will be available in the `artifacts` directory.

## Tests

The project includes deterministic tests that do not require authenticated provider CLI sessions.

Current coverage includes:

- Macro Deck integration surface
- Public variable count
- Public variable IDs
- Public variable names
- Variable type validation
- Duplicate variable protection
- Absence of plugin actions
- Codex rate limit parsing
- Claude usage parsing
- Gemini usage parsing
- Invalid or incomplete provider responses

Run all tests with:

```bash
dotnet test
```

## Platform Support

Currently tested on:

- Linux x64
- Macro Deck 3 beta
- .NET 10

Other platforms are not currently packaged or officially tested.

## Known Limitations

- Provider integrations depend on the behavior and output formats of their respective CLI tools.
- Updates to Codex CLI, Claude Code, or Antigravity may require changes to the plugin.
- Claude usage monitoring depends on Claude Code's local usage cache.
- Gemini support currently depends on Antigravity CLI.
- Only Linux x64 is currently packaged and tested.
- Macro Deck 3 is currently in beta, so plugin APIs may change.
- Provider usage information is limited to what the corresponding CLI exposes.

## Project Status

AI Usage is an early public release.

Core monitoring has been tested with:

- OpenAI Codex
- Anthropic Claude Code
- Google Gemini via Antigravity

The plugin currently exposes six consolidated card variables and no Macro Deck actions.

The current architecture prioritizes:

- Lightweight Macro Deck variable reads
- Background provider refreshes
- Last valid snapshot preservation
- Deterministic parsing
- Minimal credential exposure
- Clean process lifecycle management

## Contributing

Issues, bug reports, and pull requests are welcome.

When reporting a provider related issue, include:

- Operating system
- Macro Deck version
- AI Usage plugin version
- Relevant CLI version
- Relevant error output

Do not include:

- Access tokens
- API keys
- Passwords
- Authentication cookies
- Private account information
- Full provider configuration files containing sensitive information

## Disclaimer

AI Usage is an unofficial community project.

It is not affiliated with, endorsed by, or sponsored by OpenAI, Anthropic, Google, or Macro Deck.

Product names and trademarks belong to their respective owners.

## License

MIT License. See [LICENSE](LICENSE).