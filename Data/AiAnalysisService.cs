using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Caching.Memory;
using propro.Localization;

/// <summary>
/// AI tahlili (Claude API). appsettings.json → "Ai:ApiKey" (yoki ANTHROPIC_API_KEY muhit o'zgaruvchisi) berilmasa
/// o'chiq bo'ladi — kabinetda ichki (qoidaga asoslangan) tahlil ko'rsatiladi.
/// Natija bir xil statistika uchun 6 soat keshlanadi (keraksiz so'rov va xarajat bo'lmasligi uchun).
/// </summary>
public class AiAnalysisService
{
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AiAnalysisService> _log;

    public AiAnalysisService(IConfiguration config, IMemoryCache cache, ILogger<AiAnalysisService> log)
    {
        _config = config;
        _cache = cache;
        _log = log;
    }

    private string? ApiKey
    {
        get
        {
            var key = _config["Ai:ApiKey"];
            if (string.IsNullOrWhiteSpace(key)) key = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            return string.IsNullOrWhiteSpace(key) ? null : key;
        }
    }

    public bool IsEnabled => ApiKey != null;

    private string Model => string.IsNullOrWhiteSpace(_config["Ai:Model"]) ? "claude-opus-5" : _config["Ai:Model"]!;

    /// <summary>Tanlangan tilda qisqa shaxsiy tahlil matni. Xato yoki o'chiq bo'lsa — null.</summary>
    public async Task<string?> AnalyzeAsync(UserStats s, string lang, CancellationToken ct = default)
    {
        if (ApiKey is not string key) return null;

        var summary = BuildSummary(s, lang);
        var cacheKey = "ai:" + s.User.Id + ":" + lang + ":" +
                       Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(summary)), 0, 8);
        if (_cache.TryGetValue(cacheKey, out string? cached)) return cached;

        string language = lang switch
        {
            Lang.Ru => "Russian",
            Lang.Uzk => "Uzbek in the Cyrillic script",
            _ => "Uzbek in the Latin script",
        };

        var system =
            "You are an experienced driving-school instructor in Uzbekistan who coaches students for the official " +
            "traffic-rules (YHQ / ПДД) theory exam: 20 questions, a student passes with at least 18 correct answers. " +
            "You receive a JSON summary of one student's practice results. Write a short, warm but honest personal analysis for the student: " +
            "how ready they are, what the numbers show, which topics and habits to focus on next, and a concrete plan for the next few days. " +
            "Base every statement on the numbers given - do not invent data. " +
            $"Write in {language}. Use plain text: 5-7 short lines, each starting with \"• \". No headings, no markdown.";

        try
        {
            var client = new AnthropicClient { ApiKey = key };
            var response = await client.Messages.Create(new MessageCreateParams
            {
                Model = Model,
                MaxTokens = 4000,
                OutputConfig = new OutputConfig { Effort = Effort.Low },
                System = system,
                Messages = [new() { Role = Role.User, Content = summary }],
            }, ct);

            if (response.StopReason == "refusal")
            {
                _log.LogWarning("AI tahlili rad etildi (foydalanuvchi {UserId})", s.User.Id);
                return null;
            }

            var text = string.Join("\n", response.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text)).Trim();
            if (text.Length == 0) return null;

            _cache.Set(cacheKey, text, TimeSpan.FromHours(6));
            return text;
        }
        catch (AnthropicRateLimitException ex)
        {
            _log.LogWarning(ex, "AI tahlili: so'rovlar chegarasi");
        }
        catch (AnthropicApiException ex)
        {
            _log.LogError(ex, "AI tahlili: API xatosi");
        }
        catch (AnthropicIOException ex)
        {
            _log.LogError(ex, "AI tahlili: tarmoq xatosi");
        }
        return null;
    }

    /// <summary>Modelga yuboriladigan statistika (shaxsiy ma'lumotlarsiz — faqat raqamlar va mavzular).</summary>
    private static string BuildSummary(UserStats s, string lang)
    {
        object Pct(double v) => Math.Round(v * 100, 1);

        var data = new
        {
            questionBankSize = s.BankSize,
            testsTaken = s.TotalAttempts,
            questionsAnswered = s.TotalAnswered,
            overallAccuracyPercent = Pct(s.Accuracy),
            last200AccuracyPercent = Pct(s.RecentAccuracy),
            accuracyChangeLast100VsPrevious100Points = s.AccuracyTrend is double t ? Math.Round(t * 100, 1) : (double?)null,
            examLikeTestsTaken = s.ExamAttempts,
            last5ExamsPassRatePercent = Pct(s.ExamPassRate),
            coveragePercentOfBankAnsweredCorrectly = Pct(s.Coverage),
            readinessScore0to100 = s.ReadinessScore,
            isReady = s.IsReady,
            estimatedDaysUntilReady = s.DaysToReady,
            activeMistakes = s.ActiveMistakes,
            fixedMistakes = s.ResolvedMistakes,
            practiceStreakDays = s.StreakDays,
            daysSinceLastPractice = s.LastActivity is DateTime last ? (int)(DateTime.UtcNow - last).TotalDays : (int?)null,
            studyMinutesTotal = s.StudyMinutes,
            byTestType = s.ByType.Select(x => new { type = x.Type, attempts = x.Attempts, avgPercent = Math.Round(x.AvgPercent, 1), passed = x.Passed }),
            weakestTopics = s.WeakTopics.Select(x => new { topic = Topics.Display(x.Topic, lang), answered = x.Answered, accuracyPercent = Pct(x.Accuracy) }),
            untouchedTopics = s.UntouchedTopics.Take(10).Select(x => Topics.Display(x.Topic, lang)),
        };

        return JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });
    }
}
