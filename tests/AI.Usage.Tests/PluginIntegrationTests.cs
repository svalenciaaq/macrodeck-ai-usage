using MacroDeck.Sdk.Variables;
using NUnit.Framework;
using Serilog;

namespace AI.Usage.Tests;

[TestFixture]
public sealed class PluginIntegrationTests
{
    private static readonly string[] ExpectedVariableIds =
    [
        "codex-5h-card",
        "codex-week-card",
        "claude-5h-card",
        "claude-week-card",
        "gemini-5h-card",
        "gemini-week-card"
    ];

    private static readonly string[] ExpectedVariableNames =
    [
        "ai_usage_codex_5h_card",
        "ai_usage_codex_week_card",
        "ai_usage_claude_5h_card",
        "ai_usage_claude_week_card",
        "ai_usage_gemini_5h_card",
        "ai_usage_gemini_week_card"
    ];

    private static PluginIntegration CreateIntegration(
        out Serilog.Core.Logger logger)
    {
        logger =
            new LoggerConfiguration()
                .CreateLogger();

        return new PluginIntegration(logger);
    }

    [Test]
    public void The_plugin_exposes_no_actions()
    {
        var integration =
            CreateIntegration(out var logger);

        using (logger)
        {
            Assert.That(
                integration.Actions,
                Is.Empty
            );
        }
    }

    [Test]
    public void The_plugin_exposes_exactly_six_variables()
    {
        var integration =
            CreateIntegration(out var logger);

        using (logger)
        {
            Assert.That(
                integration.Variables,
                Has.Count.EqualTo(6)
            );
        }
    }

    [Test]
    public void The_plugin_exposes_the_expected_variable_ids()
    {
        var integration =
            CreateIntegration(out var logger);

        using (logger)
        {
            var ids =
                integration.Variables
                    .Select(variable => variable.Id)
                    .ToArray();

            Assert.That(
                ids,
                Is.EquivalentTo(ExpectedVariableIds)
            );
        }
    }

    [Test]
    public void The_plugin_exposes_the_expected_variable_names()
    {
        var integration =
            CreateIntegration(out var logger);

        using (logger)
        {
            var names =
                integration.Variables
                    .Select(variable => variable.Name)
                    .ToArray();

            Assert.That(
                names,
                Is.EquivalentTo(ExpectedVariableNames)
            );
        }
    }

    [Test]
    public void All_variables_are_text_variables()
    {
        var integration =
            CreateIntegration(out var logger);

        using (logger)
        {
            Assert.That(
                integration.Variables.All(
                    variable =>
                        variable.Type == VariableType.Text
                ),
                Is.True
            );
        }
    }

    [Test]
    public void Variable_ids_are_unique()
    {
        var integration =
            CreateIntegration(out var logger);

        using (logger)
        {
            var ids =
                integration.Variables
                    .Select(variable => variable.Id)
                    .ToArray();

            Assert.That(
                ids.Distinct().Count(),
                Is.EqualTo(ids.Length)
            );
        }
    }

    [Test]
    public void Variable_names_are_unique()
    {
        var integration =
            CreateIntegration(out var logger);

        using (logger)
        {
            var names =
                integration.Variables
                    .Select(variable => variable.Name)
                    .ToArray();

            Assert.That(
                names.Distinct().Count(),
                Is.EqualTo(names.Length)
            );
        }
    }
}
