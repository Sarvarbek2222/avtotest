using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

/// <summary>
/// Savollar bazasi xotirada (tezlik uchun): test ochilganda bazaga murojaat qilinmaydi.
/// Admin savol qo'shsa / tahrirlasa / o'chirsa — Invalidate() chaqiriladi va keyingi so'rovda qayta yuklanadi.
/// Keshdagi obyektlar faqat o'qiladi (QuestionJson.From ularni o'zgartirmaydi).
/// </summary>
public class QuestionBank
{
    private const string Key = "question-bank";
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private readonly IMemoryCache _cache;
    private readonly IServiceScopeFactory _scopes;

    public QuestionBank(IMemoryCache cache, IServiceScopeFactory scopes)
    {
        _cache = cache;
        _scopes = scopes;
    }

    /// <summary>Barcha savollar (variantlari bilan), Id bo'yicha tartiblangan.</summary>
    public async Task<IReadOnlyList<Question>> AllAsync()
    {
        if (_cache.TryGetValue(Key, out IReadOnlyList<Question>? cached) && cached != null) return cached;

        await Gate.WaitAsync();
        try
        {
            if (_cache.TryGetValue(Key, out cached) && cached != null) return cached;

            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var list = await db.Questions.AsNoTracking()
                .Include(q => q.Options)
                .AsSplitQuery()
                .OrderBy(q => q.Id)
                .ToListAsync();

            _cache.Set(Key, (IReadOnlyList<Question>)list, TimeSpan.FromMinutes(30));
            return list;
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <summary>Tasodifiy tartibda n ta savol (filter bilan yoki filtersiz).</summary>
    public async Task<List<Question>> RandomAsync(int take, Func<Question, bool>? filter = null)
    {
        var all = await AllAsync();
        IEnumerable<Question> source = filter == null ? all : all.Where(filter);
        var arr = source.ToArray();
        Random.Shared.Shuffle(arr);
        return arr.Take(take).ToList();
    }

    public void Invalidate() => _cache.Remove(Key);
}
