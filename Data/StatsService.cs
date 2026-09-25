using Microsoft.EntityFrameworkCore;

/// <summary>
/// Foydalanuvchi natijalarini tahlil qiladi: tayyorlik darajasi, imtihonga tayyor bo'lish sanasi taxmini,
/// kuchsiz mavzular, xatolar va tavsiyalar. Kabinet (foydalanuvchi) va admin paneli bir xil ma'lumotni ko'radi.
///
/// Tayyorlik modeli (0–100):
///   50% — oxirgi 200 ta javob aniqligi (90% = to'liq ball),
///   30% — oxirgi 5 ta imtihon ko'rinishidagi testdan (real, 20 savol, bilet) o'tish ulushi,
///   20% — savollar bazasini qamrab olish (bazaning 70% i to'g'ri ishlangan = to'liq ball).
/// "Tayyor": aniqlik ≥ 90%, oxirgi 3 ta imtihon testi o'tilgan, qamrov ≥ 70%, kamida 100 ta javob.
/// </summary>
public class StatsService
{
    public const double TargetAccuracy = 0.90;
    public const double TargetCoverage = 0.70;
    public const int MinAnswersForForecast = 50;
    public const int MinAnswersForReady = 100;

    private readonly AppDbContext _db;
    public StatsService(AppDbContext db) => _db = db;

    public async Task<UserStats?> GetUserStatsAsync(int userId)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return null;

        var attempts = await _db.TestAttempts.AsNoTracking()
            .Include(a => a.Answers)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.FinishedAt)
            .ToListAsync();

        var mistakes = await _db.UserMistakes.AsNoTracking().Where(m => m.UserId == userId).ToListAsync();
        var questionTopics = await _db.Questions.AsNoTracking()
            .Select(q => new { q.Id, q.Topic }).ToDictionaryAsync(q => q.Id, q => q.Topic?.Trim());

        return Compute(user, attempts, mistakes, questionTopics);
    }

    /// <summary>Admin uchun: barcha foydalanuvchilar (rolidan qat'i nazar) qisqa ko'rinishda.</summary>
    public async Task<List<UserStats>> GetAllUserStatsAsync()
    {
        var users = await _db.Users.AsNoTracking().Where(u => u.Role == Roles.User).OrderBy(u => u.Username).ToListAsync();
        var attempts = await _db.TestAttempts.AsNoTracking().Include(a => a.Answers).ToListAsync();
        var mistakes = await _db.UserMistakes.AsNoTracking().ToListAsync();
        var questionTopics = await _db.Questions.AsNoTracking()
            .Select(q => new { q.Id, q.Topic }).ToDictionaryAsync(q => q.Id, q => q.Topic?.Trim());

        var attemptsByUser = attempts.GroupBy(a => a.UserId).ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.FinishedAt).ToList());
        var mistakesByUser = mistakes.GroupBy(m => m.UserId).ToDictionary(g => g.Key, g => g.ToList());

        return users.Select(u => Compute(u,
            attemptsByUser.GetValueOrDefault(u.Id) ?? new(),
            mistakesByUser.GetValueOrDefault(u.Id) ?? new(),
            questionTopics)).ToList();
    }

    /// <summary>Admin uchun: eng ko'p xato qilinadigan savollar va mavzular.</summary>
    public async Task<PlatformStats> GetPlatformStatsAsync()
    {
        var since7 = DateTime.UtcNow.AddDays(-7);
        var ps = new PlatformStats
        {
            TotalUsers = await _db.Users.CountAsync(u => u.Role == Roles.User),
            TotalQuestions = await _db.Questions.CountAsync(),
            TotalAttempts = await _db.TestAttempts.CountAsync(a => !a.IsErrorReview),
            Attempts7d = await _db.TestAttempts.CountAsync(a => !a.IsErrorReview && a.FinishedAt >= since7),
            ActiveUsers7d = await _db.TestAttempts.Where(a => a.FinishedAt >= since7).Select(a => a.UserId).Distinct().CountAsync(),
        };

        var answerStats = await _db.TestAnswers.AsNoTracking()
            .Where(a => a.SelectedOptionId != null)
            .GroupBy(a => a.QuestionId)
            .Select(g => new { QuestionId = g.Key, Total = g.Count(), Wrong = g.Count(x => !x.IsCorrect) })
            .ToListAsync();

        ps.TotalAnswers = answerStats.Sum(a => a.Total);
        ps.OverallAccuracy = ps.TotalAnswers > 0 ? 1.0 - (double)answerStats.Sum(a => a.Wrong) / ps.TotalAnswers : 0;

        var examLike = await _db.TestAttempts.AsNoTracking()
            .Where(a => !a.IsErrorReview && TestTypes.ExamLike.Contains(a.TestType))
            .Select(a => a.Passed).ToListAsync();
        ps.ExamPassRate = examLike.Count > 0 ? (double)examLike.Count(p => p) / examLike.Count : 0;

        var hardIds = answerStats.Where(a => a.Total >= 3)
            .OrderByDescending(a => (double)a.Wrong / a.Total).ThenByDescending(a => a.Total)
            .Take(10).ToList();
        var ids = hardIds.Select(h => h.QuestionId).ToList();
        var qs = await _db.Questions.AsNoTracking().Where(q => ids.Contains(q.Id)).ToDictionaryAsync(q => q.Id);
        ps.HardestQuestions = hardIds.Where(h => qs.ContainsKey(h.QuestionId))
            .Select(h => new HardQuestion(qs[h.QuestionId], h.Total, h.Wrong)).ToList();

        var topicOf = await _db.Questions.AsNoTracking().Select(q => new { q.Id, q.Topic }).ToDictionaryAsync(q => q.Id, q => q.Topic?.Trim());
        ps.Topics = answerStats
            .Where(a => topicOf.TryGetValue(a.QuestionId, out var t) && !string.IsNullOrEmpty(t))
            .GroupBy(a => topicOf[a.QuestionId]!)
            .Select(g => new TopicStat(g.Key, g.Sum(x => x.Total), g.Sum(x => x.Total - x.Wrong)))
            .OrderBy(t => t.Accuracy).ToList();

        return ps;
    }

    private static UserStats Compute(AppUser user, List<TestAttempt> attempts, List<UserMistake> mistakes,
        Dictionary<int, string?> questionTopics)
    {
        var s = new UserStats { User = user, BankSize = questionTopics.Count };
        var today = DateTime.UtcNow.Date;

        var main = attempts.Where(a => !a.IsErrorReview).ToList();   // statistika uchun
        s.Attempts = main;
        s.AllAttemptsCount = attempts.Count;
        s.TotalAttempts = main.Count;
        s.LastActivity = attempts.FirstOrDefault()?.FinishedAt;

        // Javoblar (eng yangisi birinchi)
        var answered = attempts
            .SelectMany(a => a.Answers.Where(x => x.SelectedOptionId != null).Select(x => (a.FinishedAt, x.QuestionId, x.IsCorrect)))
            .OrderByDescending(x => x.FinishedAt).ToList();

        s.TotalAnswered = answered.Count;
        s.TotalCorrect = answered.Count(x => x.IsCorrect);
        s.Accuracy = s.TotalAnswered > 0 ? (double)s.TotalCorrect / s.TotalAnswered : 0;
        s.AveragePercent = main.Count > 0 ? main.Average(a => a.Percent) : 0;
        s.StudyMinutes = (int)attempts.Sum(a => Math.Clamp((a.FinishedAt - a.StartedAt).TotalMinutes, 0, 180));

        var recent = answered.Take(200).ToList();
        s.RecentAccuracy = recent.Count > 0 ? (double)recent.Count(x => x.IsCorrect) / recent.Count : 0;
        var prev = answered.Skip(100).Take(100).ToList();
        var last100 = answered.Take(100).ToList();
        if (last100.Count >= 30 && prev.Count >= 30)
            s.AccuracyTrend = (double)last100.Count(x => x.IsCorrect) / last100.Count - (double)prev.Count(x => x.IsCorrect) / prev.Count;

        // Test turlari bo'yicha
        s.ByType = main.GroupBy(a => a.TestType)
            .Select(g => new TypeStat(g.Key, g.Count(), g.Average(a => a.Percent), g.Max(a => a.Percent), g.Count(a => a.Passed), g.Max(a => a.FinishedAt)))
            .OrderBy(t => Array.IndexOf(TestTypes.All, t.Type)).ToList();

        // Imtihon ko'rinishidagi testlar
        var exams = main.Where(a => TestTypes.ExamLike.Contains(a.TestType)).ToList();
        var last5 = exams.Take(5).ToList();
        s.ExamAttempts = exams.Count;
        s.ExamPassRate = last5.Count > 0 ? (double)last5.Count(a => a.Passed) / last5.Count : 0;
        s.LastExamsPassed = exams.Count >= 3 && exams.Take(3).All(a => a.Passed);

        // Qamrov: to'g'ri ishlangan turli savollar
        var correctSet = answered.Where(x => x.IsCorrect).Select(x => x.QuestionId).ToHashSet();
        s.DistinctCorrect = correctSet.Count;
        s.Coverage = s.BankSize > 0 ? (double)s.DistinctCorrect / s.BankSize : 0;

        // Xatolar
        s.ActiveMistakes = mistakes.Count(m => m.ResolvedAt == null);
        s.ResolvedMistakes = mistakes.Count(m => m.ResolvedAt != null);
        s.Mistakes = mistakes.Where(m => m.ResolvedAt == null).OrderByDescending(m => m.LastWrongAt).ToList();

        // Mavzular (Topics.All tartibida, keyin boshqalar)
        var byTopic = answered.Where(x => questionTopics.TryGetValue(x.QuestionId, out var t) && !string.IsNullOrEmpty(t))
            .GroupBy(x => questionTopics[x.QuestionId]!)
            .ToDictionary(g => g.Key, g => new TopicStat(g.Key, g.Count(), g.Count(x => x.IsCorrect)));
        var bankTopics = questionTopics.Values.Where(t => !string.IsNullOrEmpty(t)).Select(t => t!).ToHashSet();
        s.Topics = Topics.All.Where(bankTopics.Contains).Concat(bankTopics.Except(Topics.All).OrderBy(t => t))
            .Select(t => byTopic.GetValueOrDefault(t) ?? new TopicStat(t, 0, 0)).ToList();
        s.WeakTopics = s.Topics.Where(t => t.Answered >= 3 && t.Accuracy < 0.8)
            .OrderBy(t => t.Accuracy).ThenByDescending(t => t.Answered).Take(5).ToList();
        s.UntouchedTopics = s.Topics.Where(t => t.Answered == 0).ToList();

        // Kunlik taraqqiyot (oxirgi 30 kun)
        var byDay = answered.GroupBy(x => x.FinishedAt.ToLocalTime().Date)
            .ToDictionary(g => g.Key, g => (Count: g.Count(), Correct: g.Count(x => x.IsCorrect)));
        var localToday = DateTime.Now.Date;
        s.Daily = Enumerable.Range(0, 30).Select(i => localToday.AddDays(i - 29))
            .Select(d => byDay.TryGetValue(d, out var v) ? new DayStat(d, v.Count, v.Correct) : new DayStat(d, 0, 0))
            .ToList();

        // Faol kunlar ketma-ketligi
        // (bugun hali mashq qilinmagan bo'lsa — kechagi kundan boshlab sanaladi)
        int streak = 0;
        var day = byDay.ContainsKey(localToday) ? localToday : localToday.AddDays(-1);
        while (byDay.ContainsKey(day) && streak < 366) { streak++; day = day.AddDays(-1); }
        s.StreakDays = streak;

        ComputeReadiness(s, answered.Select(x => (x.FinishedAt, x.QuestionId, x.IsCorrect)).ToList());
        BuildInsights(s);
        return s;
    }

    private static void ComputeReadiness(UserStats s, List<(DateTime At, int QuestionId, bool IsCorrect)> answered)
    {
        double accScore = Math.Min(s.RecentAccuracy / TargetAccuracy, 1);
        double covScore = Math.Min(s.Coverage / TargetCoverage, 1);
        s.ReadinessScore = (int)Math.Round(100 * (0.5 * accScore + 0.3 * s.ExamPassRate + 0.2 * covScore));
        s.AccuracyScore = (int)Math.Round(100 * accScore);
        s.ExamScore = (int)Math.Round(100 * s.ExamPassRate);
        s.CoverageScore = (int)Math.Round(100 * covScore);

        s.NeedMoreData = s.TotalAnswered < MinAnswersForForecast;
        s.IsReady = s.TotalAnswered >= MinAnswersForReady && s.RecentAccuracy >= TargetAccuracy &&
                    s.LastExamsPassed && s.Coverage >= TargetCoverage;
        if (s.IsReady) { s.ReadinessScore = Math.Max(s.ReadinessScore, 95); s.DaysToReady = 0; return; }
        if (s.NeedMoreData) return;

        // 1) Aniqlik tendensiyasi: faol kunlar bo'yicha chiziqli regressiya (oxirgi 21 kun, kuniga ≥ 10 javob)
        var sinceUtc = DateTime.UtcNow.AddDays(-21);
        var originLocal = DateTime.Now.Date.AddDays(-21);
        var points = answered.Where(a => a.At >= sinceUtc)
            .GroupBy(a => a.At.ToLocalTime().Date)
            .Where(g => g.Count() >= 10)
            .Select(g => (X: (g.Key - originLocal).TotalDays, Y: (double)g.Count(a => a.IsCorrect) / g.Count()))
            .OrderBy(p => p.X).ToList();

        int? accDays = null;
        if (s.RecentAccuracy >= TargetAccuracy) accDays = 0;
        else if (points.Count >= 3)
        {
            double mx = points.Average(p => p.X), my = points.Average(p => p.Y);
            double sxx = points.Sum(p => (p.X - mx) * (p.X - mx));
            double slope = sxx > 0 ? points.Sum(p => (p.X - mx) * (p.Y - my)) / sxx : 0;
            s.DailyGrowth = slope;
            if (slope > 0.002) accDays = (int)Math.Ceiling((TargetAccuracy - s.RecentAccuracy) / slope);
        }

        // 2) Qamrov sur'ati: faol kunlarda yangi to'g'ri ishlangan savollar soni
        int? covDays = null;
        int needCorrect = (int)Math.Ceiling(TargetCoverage * s.BankSize) - s.DistinctCorrect;
        if (needCorrect <= 0) covDays = 0;
        else
        {
            var firstCorrect = answered.Where(a => a.IsCorrect).GroupBy(a => a.QuestionId)
                .Select(g => g.Min(a => a.At)).Where(d => d >= DateTime.UtcNow.AddDays(-14)).ToList();
            int activeDays = answered.Where(a => a.At >= DateTime.UtcNow.AddDays(-14))
                .Select(a => a.At.ToLocalTime().Date).Distinct().Count();
            double pace = activeDays > 0 ? (double)firstCorrect.Count / 14 : 0; // kalendar kuni hisobida
            if (pace > 0) covDays = (int)Math.Ceiling(needCorrect / pace);
        }

        // 3) Imtihon testlari: tayyor bo'lsa ham kamida 3 ta real imtihonni o'tish kerak (~2 kun)
        int examDays = s.LastExamsPassed ? 0 : 2;

        if (accDays is int a && covDays is int c)
        {
            s.DaysToReady = Math.Clamp(Math.Max(Math.Max(a, c), examDays), 1, 365);
            s.PredictedReadyDate = DateTime.Now.Date.AddDays(s.DaysToReady.Value);
        }
    }

    private static void BuildInsights(UserStats s)
    {
        var list = s.Insights;
        if (s.TotalAnswered == 0) { list.Add(new Insight("insight.start", InsightLevel.Info)); return; }
        if (s.NeedMoreData)
            list.Add(new Insight("insight.needData", InsightLevel.Info, ("n", MinAnswersForForecast - s.TotalAnswered)));
        if (s.IsReady)
            list.Add(new Insight("insight.ready", InsightLevel.Good));
        else if (s.PredictedReadyDate != null)
            list.Add(new Insight("insight.forecast", InsightLevel.Info, ("days", s.DaysToReady), ("date", s.PredictedReadyDate.Value.ToString("d MMMM"))));
        else if (!s.NeedMoreData)
            list.Add(new Insight("insight.noForecast", InsightLevel.Warn));

        if (s.AccuracyTrend is double tr)
        {
            int pp = (int)Math.Round(tr * 100);
            if (pp >= 5) list.Add(new Insight("insight.improving", InsightLevel.Good, ("n", pp)));
            else if (pp <= -5) list.Add(new Insight("insight.declining", InsightLevel.Warn, ("n", -pp)));
        }

        if (s.WeakTopics.Count > 0)
            list.Add(new Insight("insight.weakTopics", InsightLevel.Warn, ("n", s.WeakTopics.Count)) { Topics = s.WeakTopics.Take(3).Select(t => t.Topic).ToList() });
        if (s.UntouchedTopics.Count > 0)
            list.Add(new Insight("insight.untouched", InsightLevel.Info, ("n", s.UntouchedTopics.Count)));
        if (s.ActiveMistakes > 0)
            list.Add(new Insight("insight.mistakes", InsightLevel.Warn, ("n", s.ActiveMistakes)));

        if (s.ExamAttempts < 3)
            list.Add(new Insight("insight.examPractice", InsightLevel.Info));
        else if (s.ExamPassRate < 0.6)
            list.Add(new Insight("insight.examLow", InsightLevel.Warn, ("n", (int)Math.Round(s.ExamPassRate * 100))));

        if (s.Coverage < 0.5 && !s.NeedMoreData)
            list.Add(new Insight("insight.coverage", InsightLevel.Info, ("n", (int)Math.Round(s.Coverage * 100))));

        if (s.LastActivity is DateTime last)
        {
            int days = (int)(DateTime.UtcNow - last).TotalDays;
            if (days >= 3) list.Add(new Insight("insight.inactive", InsightLevel.Warn, ("n", days)));
        }
        if (s.StreakDays >= 3)
            list.Add(new Insight("insight.streak", InsightLevel.Good, ("n", s.StreakDays)));
    }
}

public class UserStats
{
    public AppUser User { get; set; } = null!;
    public int BankSize { get; set; }

    public List<TestAttempt> Attempts { get; set; } = new();
    public int TotalAttempts { get; set; }
    public int AllAttemptsCount { get; set; }
    public DateTime? LastActivity { get; set; }

    public int TotalAnswered { get; set; }
    public int TotalCorrect { get; set; }
    public double Accuracy { get; set; }
    public double RecentAccuracy { get; set; }
    public double? AccuracyTrend { get; set; }
    public double AveragePercent { get; set; }
    public int StudyMinutes { get; set; }
    public int StreakDays { get; set; }

    public List<TypeStat> ByType { get; set; } = new();
    public int ExamAttempts { get; set; }
    public double ExamPassRate { get; set; }
    public bool LastExamsPassed { get; set; }

    public int DistinctCorrect { get; set; }
    public double Coverage { get; set; }

    public int ActiveMistakes { get; set; }
    public int ResolvedMistakes { get; set; }
    public List<UserMistake> Mistakes { get; set; } = new();

    public List<TopicStat> Topics { get; set; } = new();
    public List<TopicStat> WeakTopics { get; set; } = new();
    public List<TopicStat> UntouchedTopics { get; set; } = new();
    public List<DayStat> Daily { get; set; } = new();

    public int ReadinessScore { get; set; }
    public int AccuracyScore { get; set; }
    public int ExamScore { get; set; }
    public int CoverageScore { get; set; }
    public bool IsReady { get; set; }
    public bool NeedMoreData { get; set; }
    public int? DaysToReady { get; set; }
    public DateTime? PredictedReadyDate { get; set; }
    public double? DailyGrowth { get; set; }

    public List<Insight> Insights { get; set; } = new();
}

public record TypeStat(string Type, int Attempts, double AvgPercent, double BestPercent, int Passed, DateTime LastAt);

public record TopicStat(string Topic, int Answered, int Correct)
{
    public double Accuracy => Answered > 0 ? (double)Correct / Answered : 0;
}

public record DayStat(DateTime Date, int Answered, int Correct)
{
    public double Accuracy => Answered > 0 ? (double)Correct / Answered : 0;
}

public enum InsightLevel { Info, Good, Warn }

/// <summary>Tavsiya: resx kaliti + parametrlar (tanlangan tilda chiqariladi).</summary>
public class Insight
{
    public Insight(string key, InsightLevel level, params (string name, object? value)[] args)
    {
        Key = key;
        Level = level;
        Args = args;
    }

    public string Key { get; }
    public InsightLevel Level { get; }
    public (string name, object? value)[] Args { get; }
    public List<string> Topics { get; set; } = new();
}

public class PlatformStats
{
    public int TotalUsers { get; set; }
    public int ActiveUsers7d { get; set; }
    public int TotalQuestions { get; set; }
    public int TotalAttempts { get; set; }
    public int Attempts7d { get; set; }
    public int TotalAnswers { get; set; }
    public double OverallAccuracy { get; set; }
    public double ExamPassRate { get; set; }
    public List<HardQuestion> HardestQuestions { get; set; } = new();
    public List<TopicStat> Topics { get; set; } = new();
}

public record HardQuestion(Question Question, int Total, int Wrong)
{
    public double WrongRate => Total > 0 ? (double)Wrong / Total : 0;
}
