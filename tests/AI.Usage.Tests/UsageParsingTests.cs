using System.Globalization;
using System.Text.Json;
using NUnit.Framework;

namespace AI.Usage.Tests;

[TestFixture]
public sealed class CodexUsageParsingTests
{
    [Test]
    public void Parse_reads_rate_limit_windows()
    {
        using var document =
            JsonDocument.Parse(
                """
                {
                  "jsonrpc": "2.0",
                  "id": 2,
                  "result": {
                    "rateLimits": {
                      "primary": {
                        "usedPercent": 25,
                        "windowDurationMins": 300,
                        "resetsAt": 1800000000
                      },
                      "secondary": {
                        "usedPercent": 40,
                        "windowDurationMins": 10080,
                        "resetsAt": 1800600000
                      },
                      "planType": "plus"
                    },
                    "rateLimitResetCredits": {
                      "availableCount": 3
                    }
                  }
                }
                """
            );

        var result =
            CodexUsageService.Parse(
                document.RootElement
            );

        Assert.That(result, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(result!.FiveHour!.UsedPercent, Is.EqualTo(25));
            Assert.That(result.FiveHour.WindowMinutes, Is.EqualTo(300));

            Assert.That(result.Weekly!.UsedPercent, Is.EqualTo(40));
            Assert.That(result.Weekly.WindowMinutes, Is.EqualTo(10080));

            Assert.That(result.PlanType, Is.EqualTo("plus"));
            Assert.That(result.ResetCredits, Is.EqualTo(3));
        });
    }

    [Test]
    public void Parse_returns_null_when_required_window_is_missing()
    {
        using var document =
            JsonDocument.Parse(
                """
                {
                  "result": {
                    "rateLimits": {
                      "primary": {
                        "usedPercent": 25,
                        "windowDurationMins": 300,
                        "resetsAt": 1800000000
                      }
                    }
                  }
                }
                """
            );

        var result =
            CodexUsageService.Parse(
                document.RootElement
            );

        Assert.That(result, Is.Null);
    }
}

[TestFixture]
public sealed class ClaudeUsageParsingTests
{
    [Test]
    public void ParseWindow_reads_claude_utilization()
    {
        using var document =
            JsonDocument.Parse(
                """
                {
                  "five_hour": {
                    "utilization": 17,
                    "resets_at": "2026-09-21T20:00:00Z"
                  }
                }
                """
            );

        var result =
            ClaudeUsageService.ParseWindow(
                document.RootElement,
                "five_hour",
                300
            );

        Assert.That(result, Is.Not.Null);

        var expectedReset =
            DateTimeOffset.Parse(
                "2026-09-21T20:00:00Z",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind
            ).ToUnixTimeSeconds();

        Assert.Multiple(() =>
        {
            Assert.That(result!.UsedPercent, Is.EqualTo(17));
            Assert.That(result.WindowMinutes, Is.EqualTo(300));
            Assert.That(result.ResetsAt, Is.EqualTo(expectedReset));
        });
    }

    [Test]
    public void ParseWindow_returns_null_for_invalid_reset_time()
    {
        using var document =
            JsonDocument.Parse(
                """
                {
                  "five_hour": {
                    "utilization": 17,
                    "resets_at": "invalid"
                  }
                }
                """
            );

        var result =
            ClaudeUsageService.ParseWindow(
                document.RootElement,
                "five_hour",
                300
            );

        Assert.That(result, Is.Null);
    }
}

[TestFixture]
public sealed class GeminiUsageParsingTests
{
    [Test]
    public void Parse_reads_gemini_model_buckets()
    {
        const string json =
            """
            {
              "command": {
                "data": {
                  "groups": [
                    {
                      "name": "Gemini Models",
                      "buckets": [
                        {
                          "window": "5h",
                          "remaining_fraction": 0.80,
                          "reset_time": "2026-09-21T20:00:00Z"
                        },
                        {
                          "window": "weekly",
                          "remaining_fraction": 0.65,
                          "reset_time": "2026-09-25T20:00:00Z"
                        }
                      ]
                    }
                  ]
                }
              }
            }
            """;

        var fetchedAt =
            new DateTimeOffset(
                2026,
                9,
                21,
                12,
                0,
                0,
                TimeSpan.Zero
            );

        var result =
            GeminiUsageService.Parse(
                json,
                fetchedAt
            );

        Assert.That(result, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(
                result!.FiveHour.UsedPercent,
                Is.EqualTo(20).Within(0.001)
            );

            Assert.That(
                result.FiveHour.WindowMinutes,
                Is.EqualTo(300)
            );

            Assert.That(
                result.Weekly.UsedPercent,
                Is.EqualTo(35).Within(0.001)
            );

            Assert.That(
                result.Weekly.WindowMinutes,
                Is.EqualTo(10080)
            );

            Assert.That(
                result.FetchedAt,
                Is.EqualTo(fetchedAt)
            );
        });
    }

    [Test]
    public void Parse_ignores_unrelated_groups()
    {
        const string json =
            """
            {
              "command": {
                "data": {
                  "groups": [
                    {
                      "name": "Other Models",
                      "buckets": []
                    }
                  ]
                }
              }
            }
            """;

        var result =
            GeminiUsageService.Parse(
                json,
                DateTimeOffset.UtcNow
            );

        Assert.That(result, Is.Null);
    }
}
