using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using propro;
using propro.Localization;

/// <summary>Savollar va javob variantlarini boshqarish — faqat super admin.</summary>
[Authorize(Roles = Roles.SuperAdmin)]
public class QuestionController : Controller
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly IStringLocalizer<SharedResource> _t;
    private readonly QuestionBank _bank;

    public QuestionController(AppDbContext context, IWebHostEnvironment env, IStringLocalizer<SharedResource> t, QuestionBank bank)
    {
        _context = context;
        _env = env;
        _t = t;
        _bank = bank;
    }

    /// <summary>Savol o'zgarganda test sahifalari keshini yangilash.</summary>
    public override void OnActionExecuted(Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext context)
    {
        if (HttpMethods.IsPost(Request.Method)) _bank.Invalidate();
        base.OnActionExecuted(context);
    }

    public async Task<IActionResult> Index()
    {
        var questions = await _context.Questions.Include(q => q.Options).ToListAsync();
        return View(questions);
    }

    public async Task<IActionResult> Index1()
    {
        var questions = await _context.Questions.ToListAsync();

        // Jami savollar
        ViewBag.TotalQuestions = questions.Count;
        // Tarjimalar holati
        ViewBag.WithRu = questions.Count(q => !string.IsNullOrWhiteSpace(q.QuestionRU));
        ViewBag.WithUzk = questions.Count(q => !string.IsNullOrWhiteSpace(q.QuestionUZK));

        return View();
    }

    public IActionResult Create()
    {
        var model = new Question
        {
            Options = new List<Option>()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Question question, int? CorrectIndex)
    {
        if (!PrepareAndValidate(question, CorrectIndex))
            return View(question);

        // Rasm upload
        if (question.ImageFile != null)
            question.ImageUrl = await SaveImageAsync(question.ImageFile);

        _context.Questions.Add(question);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var question = await _context.Questions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null) return NotFound();
        return View(question);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Question question, int? CorrectIndex, bool RemoveImage = false)
    {
        var dbQuestion = await _context.Questions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == question.Id);

        if (dbQuestion == null) return NotFound();

        if (!PrepareAndValidate(question, CorrectIndex))
        {
            question.ImageUrl = dbQuestion.ImageUrl; // joriy rasm formada ko'rinib tursin
            return View(question);
        }

        // Savol matnlarini yangilash (3 til)
        dbQuestion.QuestionUZ = question.QuestionUZ;
        dbQuestion.QuestionRU = question.QuestionRU;
        dbQuestion.QuestionUZK = question.QuestionUZK;
        dbQuestion.ExplanationUZ = question.ExplanationUZ;
        dbQuestion.ExplanationRU = question.ExplanationRU;
        dbQuestion.ExplanationUZK = question.ExplanationUZK;
        dbQuestion.Topic = question.Topic;

        // Rasmni yangilash (agar yangi rasm tanlangan bo'lsa) yoki o'chirish
        if (question.ImageFile != null)
            dbQuestion.ImageUrl = await SaveImageAsync(question.ImageFile);
        else if (RemoveImage)
            dbQuestion.ImageUrl = null;

        // Eski variantlarni o'chirish
        _context.Options.RemoveRange(dbQuestion.Options);

        // Yangi variantlarni qo'shish
        foreach (var o in question.Options)
        {
            dbQuestion.Options.Add(new Option
            {
                OptionUZ = o.OptionUZ,
                OptionRU = o.OptionRU,
                OptionUZK = o.OptionUZK,
                IsCorrect = o.IsCorrect
            });
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Formani tozalaydi va tekshiradi:
    ///  • to'liq bo'sh variantlar olib tashlanadi, to'g'ri javob belgilanadi;
    ///  • o'zbek (lotin) matni majburiy, rus/kirill ixtiyoriy;
    ///  • kirill maydoni bo'sh bo'lsa — lotin matnidan avtomatik o'giriladi.
    /// </summary>
    private bool PrepareAndValidate(Question question, int? correctIndex)
    {
        // Model binding o'zining (inglizcha) xabarlarini qo'shadi — o'rniga o'zimiz tekshiramiz
        ModelState.Clear();

        question.QuestionUZ = (question.QuestionUZ ?? "").Trim();
        question.QuestionRU = Clean(question.QuestionRU);
        question.QuestionUZK = Clean(question.QuestionUZK);
        question.ExplanationUZ = Clean(question.ExplanationUZ);
        question.ExplanationRU = Clean(question.ExplanationRU);
        question.ExplanationUZK = Clean(question.ExplanationUZK);
        question.Topic = Clean(question.Topic);

        // To'g'ri javobni bo'sh variantlarni olib tashlashdan oldin belgilaymiz
        for (int i = 0; i < question.Options.Count; i++)
            question.Options[i].IsCorrect = i == correctIndex;

        question.Options = question.Options
            .Where(o => !string.IsNullOrWhiteSpace(o.OptionUZ) ||
                        !string.IsNullOrWhiteSpace(o.OptionRU) ||
                        !string.IsNullOrWhiteSpace(o.OptionUZK))
            .ToList();

        foreach (var o in question.Options)
        {
            o.OptionUZ = (o.OptionUZ ?? "").Trim();
            o.OptionRU = Clean(o.OptionRU);
            o.OptionUZK = Clean(o.OptionUZK);
        }

        if (string.IsNullOrWhiteSpace(question.QuestionUZ))
            ModelState.AddModelError("", _t["err.questionUzRequired"]);
        if (question.Options.Count < 2)
            ModelState.AddModelError("", _t["err.minOptions"]);
        if (question.Options.Any(o => string.IsNullOrWhiteSpace(o.OptionUZ)))
            ModelState.AddModelError("", _t["err.optionUzRequired"]);
        if (!question.Options.Any(o => o.IsCorrect))
            ModelState.AddModelError("", _t["err.correctRequired"]);

        if (!ModelState.IsValid) return false;

        // Kirill bo'sh qolgan bo'lsa — lotindan avtomatik transliteratsiya
        question.QuestionUZK ??= Translit.ToCyrillic(question.QuestionUZ);
        if (question.ExplanationUZ != null)
            question.ExplanationUZK ??= Translit.ToCyrillic(question.ExplanationUZ);
        foreach (var o in question.Options)
            o.OptionUZK ??= Translit.ToCyrillic(o.OptionUZ);

        return true;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<string> SaveImageAsync(IFormFile file)
    {
        var fileName = Path.GetFileName(file.FileName); // faqat nom
        var filePath = Path.Combine(_env.WebRootPath, "uploads", fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        // Faqat nomni saqlaymiz
        return fileName;
    }

    [HttpPost]
    public async Task<IActionResult> DeleteOption(int id)
    {
        var option = await _context.Options.FindAsync(id);
        if (option != null)
        {
            _context.Options.Remove(option);
            await _context.SaveChangesAsync();
        }
        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var question = await _context.Questions.FindAsync(id);
        if (question != null)
        {
            // Agar rasm mavjud bo‘lsa, faylni o‘chirish mumkin
            if (!string.IsNullOrEmpty(question.ImageUrl))
            {
                var path = Path.Combine(_env.WebRootPath, question.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}