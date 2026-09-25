using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using propro.Localization;

/// <summary>
/// Foydalanuvchining shaxsiy kabineti: natijalar, xatolar, tahlil, xatolar ustida ishlash.
/// Super admin kabinetga emas — admin paneliga (Tahlil bo'limi) yo'naltiriladi.
/// </summary>
[Authorize]
public class CabinetController : Controller
{
    public const int PageSize = 20;

    private readonly AppDbContext _db;
    private readonly StatsService _stats;
    private readonly ResultsService _results;
    private readonly AiAnalysisService _ai;

    public CabinetController(AppDbContext db, StatsService stats, ResultsService results, AiAnalysisService ai)
    {
        _db = db;
        _stats = stats;
        _results = results;
        _ai = ai;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole(Roles.SuperAdmin);

    public async Task<IActionResult> Index()
    {
        if (IsAdmin) return RedirectToAction("Index", "Analytics");
        return View(await BuildAsync(UserId));
    }

    public async Task<IActionResult> Results(string? type, int page = 1)
    {
        if (IsAdmin) return RedirectToAction("Index", "Analytics");
        var model = await BuildAsync(UserId);
        ApplyResultsFilter(model, type, page);
        return View(model);
    }

    public async Task<IActionResult> Attempt(int id)
    {
        var detail = await LoadAttemptAsync(_db, id, UserId);
        if (detail == null) return NotFound();
        return View(detail);
    }

    public async Task<IActionResult> Mistakes()
    {
        if (IsAdmin) return RedirectToAction("Index", "Analytics");
        return View(await BuildAsync(UserId, withMistakeQuestions: true));
    }

    public async Task<IActionResult> Analysis()
    {
        if (IsAdmin) return RedirectToAction("Index", "Analytics");
        return View(await BuildAsync(UserId));
    }

    /// <summary>Xatolarni qayta ishlash — test sahifalari bilan bir xil ko'rinish.</summary>
    public async Task<IActionResult> Practice()
    {
        bool any = await _db.UserMistakes.AnyAsync(m => m.UserId == UserId && m.ResolvedAt == null);
        if (!any) return RedirectToAction(nameof(Mistakes));
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetMistakeQuestions()
    {
        var questions = await _results.GetMistakeQuestionsAsync(UserId);
        return Json(questions.Select(QuestionJson.From));
    }

    /// <summary>Test sahifasidan (test-tracker.js) natijani qabul qiladi.</summary>
    [HttpPost]
    [IgnoreAntiforgeryToken] // faqat application/json qabul qilinadi (oddiy HTML forma yubora olmaydi) + SameSite=Lax cookie
    [Consumes("application/json")]
    public async Task<IActionResult> SaveResult([FromBody] SaveResultRequest request)
    {
        var result = await _results.SaveAsync(UserId, request);
        return result == null ? BadRequest() : Json(result);
    }

    /// <summary>AI tahlili (Claude). Sozlanmagan bo'lsa 404.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ai(CancellationToken ct)
    {
        if (!_ai.IsEnabled) return NotFound();
        var stats = await _stats.GetUserStatsAsync(UserId);
        if (stats == null) return NotFound();
        var text = await _ai.AnalyzeAsync(stats, Lang.Current(HttpContext), ct);
        return text == null ? StatusCode(502) : Json(new { text });
    }

    // ───────────── umumiy yordamchilar (admin paneli ham ishlatadi) ─────────────

    private async Task<CabinetModel> BuildAsync(int userId, bool withMistakeQuestions = false)
    {
        var stats = await _stats.GetUserStatsAsync(userId) ?? throw new InvalidOperationException("User not found");
        return await CabinetModel.CreateAsync(_db, stats, isAdminView: false, aiEnabled: _ai.IsEnabled, withMistakeQuestions);
    }

    public static void ApplyResultsFilter(CabinetModel model, string? type, int page)
    {
        var list = model.Stats.Attempts.AsEnumerable();
        if (TestTypes.IsValid(type)) list = list.Where(a => a.TestType == type);
        var filtered = list.ToList();

        model.FilterType = TestTypes.IsValid(type) ? type : null;
        model.TotalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PageSize));
        model.Page = Math.Clamp(page, 1, model.TotalPages);
        model.PageAttempts = filtered.Skip((model.Page - 1) * PageSize).Take(PageSize).ToList();
    }

    public static async Task<AttemptDetail?> LoadAttemptAsync(AppDbContext db, int attemptId, int? ownerId)
    {
        var attempt = await db.TestAttempts.AsNoTracking().Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == attemptId && (ownerId == null || a.UserId == ownerId));
        if (attempt == null) return null;

        var ids = attempt.Answers.Select(a => a.QuestionId).ToList();
        var questions = await db.Questions.AsNoTracking().Include(q => q.Options)
            .Where(q => ids.Contains(q.Id)).ToDictionaryAsync(q => q.Id);
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == attempt.UserId);

        return new AttemptDetail(attempt, questions, user, IsAdminView: ownerId == null);
    }
}

/// <summary>Kabinet sahifalari modeli (foydalanuvchi va admin uchun umumiy).</summary>
public class CabinetModel
{
    public UserStats Stats { get; set; } = null!;
    public bool IsAdminView { get; set; }
    public bool AiEnabled { get; set; }

    /// <summary>Savollar (xatolar ro'yxati va so'nggi urinishlar uchun).</summary>
    public Dictionary<int, Question> Questions { get; set; } = new();

    // Natijalar sahifasi
    public List<TestAttempt> PageAttempts { get; set; } = new();
    public string? FilterType { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;

    /// <summary>Havolalar uchun: urinish tafsilotlari manzili.</summary>
    public string AttemptUrl(int id) => IsAdminView ? $"/Analytics/Attempt/{id}" : $"/Cabinet/Attempt/{id}";

    public static async Task<CabinetModel> CreateAsync(AppDbContext db, UserStats stats, bool isAdminView, bool aiEnabled, bool withMistakeQuestions)
    {
        var model = new CabinetModel { Stats = stats, IsAdminView = isAdminView, AiEnabled = aiEnabled };
        model.PageAttempts = stats.Attempts.Take(CabinetController.PageSize).ToList();
        model.TotalPages = Math.Max(1, (int)Math.Ceiling(stats.Attempts.Count / (double)CabinetController.PageSize));

        if (withMistakeQuestions && stats.Mistakes.Count > 0)
        {
            var ids = stats.Mistakes.Select(m => m.QuestionId).ToList();
            model.Questions = await db.Questions.AsNoTracking().Include(q => q.Options)
                .Where(q => ids.Contains(q.Id)).ToDictionaryAsync(q => q.Id);
        }
        return model;
    }
}

public record AttemptDetail(TestAttempt Attempt, Dictionary<int, Question> Questions, AppUser User, bool IsAdminView);

/// <summary>Urinishlar jadvali partial'i uchun.</summary>
public record AttemptsTable(CabinetModel Cab, IList<TestAttempt> Items, bool Compact);

/// <summary>Rang: tayyorlik/foizga qarab.</summary>
public static class Score
{
    public static string Color(double percent) => percent >= 90 ? "var(--ca-ok)" : percent >= 60 ? "var(--ca-warn)" : "var(--ca-brand)";
    public static string BarClass(double percent) => percent >= 90 ? "ok" : percent >= 60 ? "warn" : "";
    public static string Pct(double fraction) => Math.Round(fraction * 100).ToString("0");
}
