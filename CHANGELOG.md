# Changelog

All notable changes to AI Usage will be documented in this file.

The project follows semantic versioning for public releases.

## [1.0.2] - 2026-09-21

### Added

- Public monitoring for Codex, Claude Code, and Gemini through Antigravity
- Six consolidated Macro Deck usage card variables
- Five hour and weekly usage windows
- Remaining percentage, visual usage bar, and reset countdown
- Background provider refresh with in-memory snapshots
- Last valid snapshot preservation during transient provider failures
- Deterministic parser and integration-surface tests
- Automated GitHub Actions CI
- Automated tagged release packaging and validation
- SHA256 checksum generation for release artifacts

### Changed

- Reduced the public variable surface to six consolidated card variables
- Removed template actions and unused template artifacts
- Improved provider process cleanup and failure logging

### Security

- No provider API keys are stored by the plugin
- Provider authentication remains managed by the installed CLI tools

[1.0.2]: https://github.com/svalenciaaq/macrodeck-ai-usage/releases/tag/v1.0.2
