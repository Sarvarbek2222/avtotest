using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using propro.Localization;



public class HomeController : Controller

{

    private readonly AppDbContext _context;

    public HomeController(AppDbContext context) => _context = context;



    // Super admin uchun sayt bosh sahifasi kerak emas — admin paneliga
    public IActionResult Index() =>
        User.IsInRole(Roles.SuperAdmin) ? RedirectToAction("Index1", "Question") : View();



    [HttpGet]

    public async Task<IActionResult> GetQuestions(string? lang = null)

    {

        lang = lang == null ? Lang.Current(HttpContext) : Lang.Normalize(lang);



        var questions = await _context.Questions

            .Include(q => q.Options)

            .OrderBy(q => Guid.NewGuid())

            .Take(2)

            .ToListAsync();



        return Json(questions.Select(q => new

        {

            Id = q.Id,

            Question = Lang.Pick(lang, q.QuestionUZ, q.QuestionRU, q.QuestionUZK),

            Options = q.Options

                .Select(o => new

                {

                    Id = o.Id,

                    Text = Lang.Pick(lang, o.OptionUZ, o.OptionRU, o.OptionUZK),

                    IsCorrect = o.IsCorrect

                })

                .OrderBy(x => Guid.NewGuid())

                .ToList()

        }));

    }



    /// <summary>Interfeys matnlari — barcha tillar (eski site.js uchun).</summary>

    [HttpGet]

    public IActionResult GetTexts()

    {

        return Content(Texts.ToJson(), "application/json; charset=utf-8");

    }

}

