using Microsoft.EntityFrameworkCore;

/// <summary>
/// Test natijalarini saqlash va xato savollarni kuzatish.
/// To'g'ri/xato server tomonida bazadagi variantlar bo'yicha hisoblanadi — brauzerga ishonilmaydi.
/// </summary>
public class ResultsService
{
    public const int MaxItems = 3000;
    private readonly AppDbContext _db;

    public ResultsService(AppDbContext db) => _db = db;

    public async Task<SaveResultResponse?> SaveAsync(int userId, SaveResultRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.AttemptKey) || req.AttemptKey.Length > 64) return null;
        if (!TestTypes.IsValid(req.TestType)) return null;
        if (req.Items == null || req.Items.Count == 0 || req.Items.Count > MaxItems) return null;

        // Har bir savol bir marta; mavjud bo'lmagan savollar tashlab yuboriladi
        var items = req.Items.GroupBy(i => i.QuestionId).Select(g => g.Last()).ToList();
        var qIds = items.Select(i => i.QuestionId).ToList();

        var options = await _db.Options.AsNoTracking()
            .Where(o => qIds.Contains(o.QuestionId))
            .Select(o => new { o.Id, o.QuestionId, o.IsCorrect })
            .ToListAsync();
        var existingQ = options.Select(o => o.QuestionId).ToHashSet();
        var optionById = options.ToDictionary(o => o.Id);

        var answers = new List<TestAnswer>();
        foreach (var it in items.Where(i => existingQ.Contains(i.QuestionId)))
        {
            int? sel = it.SelectedOptionId;
            bool correct = false;
            if (sel is int s && optionById.TryGetValue(s, out var opt) && opt.QuestionId == it.QuestionId)
                correct = opt.IsCorrect;
            else
                sel = null; // boshqa savolning varianti yoki noma'lum — javob berilmagan deb hisoblanadi

            answers.Add(new TestAnswer { QuestionId = it.QuestionId, SelectedOptionId = sel, IsCorrect = correct });
        }
        if (answers.Count == 0 || answers.All(a => a.SelectedOptionId == null)) return null;

        var now = DateTime.UtcNow;
        var attempt = await _db.TestAttempts.Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.AttemptKey == req.AttemptKey);

        // Oldingi holat (bir urinish qayta yuborilsa, xatolar ikki marta sanalmasin)
        var prevWrong = new HashSet<int>();
        var prevCorrect = new HashSet<int>();

        if (attempt == null)
        {
            attempt = new TestAttempt { UserId = userId, AttemptKey = req.AttemptKey, StartedAt = FromUnixMs(req.StartedAt) ?? now };
            _db.TestAttempts.Add(attempt);
        }
        else
        {
            if (attempt.UserId != userId) return null; // boshqa foydalanuvchining kaliti
            foreach (var a in attempt.Answers)
            {
                if (a.IsCorrect) prevCorrect.Add(a.QuestionId);
                else if (a.SelectedOptionId != null) prevWrong.Add(a.QuestionId);
            }
            _db.TestAnswers.RemoveRange(attempt.Answers);
            attempt.Answers.Clear();
        }

        attempt.TestType = req.TestType!;
        attempt.TestRef = string.IsNullOrWhiteSpace(req.TestRef) ? null : req.TestRef.Trim()[..Math.Min(255, req.TestRef.Trim().Length)];
        attempt.IsErrorReview = req.IsErrorReview;
        attempt.Total = answers.Count;
        attempt.Correct = answers.Count(a => a.IsCorrect);
        attempt.Skipped = answers.Count(a => a.SelectedOptionId == null);
        attempt.Wrong = attempt.Total - attempt.Correct - attempt.Skipped;
        attempt.Passed = attempt.Correct >= (int)Math.Ceiling(attempt.Total * 0.9);
        attempt.FinishedAt = now;
        if (attempt.StartedAt > now || attempt.StartedAt < now.AddHours(-12)) attempt.StartedAt = now;
        attempt.Answers.AddRange(answers);

        // ── Xato savollar ──
        var mistakes = await _db.UserMistakes.Where(m => m.UserId == userId && qIds.Contains(m.QuestionId)).ToListAsync();
        var mistakeByQ = mistakes.ToDictionary(m => m.QuestionId);

        foreach (var a in answers)
        {
            if (a.SelectedOptionId == null) continue;

            if (!a.IsCorrect)
            {
                if (prevWrong.Contains(a.QuestionId)) continue;
                if (!mistakeByQ.TryGetValue(a.QuestionId, out var m))
                {
                    m = new UserMistake { UserId = userId, QuestionId = a.QuestionId };
                    _db.UserMistakes.Add(m);
                    mistakeByQ[a.QuestionId] = m;
                }
                m.WrongCount++;
                m.LastWrongAt = now;
                m.ResolvedAt = null;
            }
            else if (!prevCorrect.Contains(a.QuestionId) &&
                     mistakeByQ.TryGetValue(a.QuestionId, out var m) && m.ResolvedAt == null)
            {
                // To'g'ri javob berildi — xato "Xatolarim" ro'yxatidan chiqadi
                m.ResolvedAt = now;
            }
        }

        await _db.SaveChangesAsync();

        var active = await _db.UserMistakes.CountAsync(m => m.UserId == userId && m.ResolvedAt == null);
        return new SaveResultResponse(attempt.Id, attempt.Correct, attempt.Total, attempt.Passed, active);
    }

    /// <summary>Foydalanuvchining faol xato savollari — "Xatolarni qayta ishlash" testi uchun.</summary>
    public async Task<List<Question>> GetMistakeQuestionsAsync(int userId, int take = 50)
    {
        var ids = await _db.UserMistakes.AsNoTracking()
            .Where(m => m.UserId == userId && m.ResolvedAt == null)
            .OrderByDescending(m => m.LastWrongAt)
            .Select(m => m.QuestionId)
            .Take(take)
            .ToListAsync();

        var questions = await _db.Questions.AsNoTracking().Include(q => q.Options)
            .Where(q => ids.Contains(q.Id))
            .ToListAsync();

        return questions.OrderBy(_ => Guid.NewGuid()).ToList();
    }

    private static DateTime? FromUnixMs(long? ms)
    {
        if (ms is not long v || v <= 0) return null;
        try { return DateTimeOffset.FromUnixTimeMilliseconds(v).UtcDateTime; }
        catch (ArgumentOutOfRangeException) { return null; }
    }
}

public class SaveResultRequest
{
    public string? AttemptKey { get; set; }
    public string? TestType { get; set; }
    public string? TestRef { get; set; }
    public bool IsErrorReview { get; set; }
    public long? StartedAt { get; set; }
    public List<SaveResultItem> Items { get; set; } = new();
}

public class SaveResultItem
{
    public int QuestionId { get; set; }
    public int? SelectedOptionId { get; set; }
}

public record SaveResultResponse(int AttemptId, int Correct, int Total, bool Passed, int ActiveMistakes);
