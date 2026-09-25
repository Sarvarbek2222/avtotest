using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace propro.Controllers
{
    public class TesthundredController : Controller
    {
        private readonly AppDbContext _context;
        private readonly QuestionBank _bank;
        public TesthundredController(AppDbContext context, QuestionBank bank) { _context = context; _bank = bank; }

        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> GetQuestions()
        {
            // Barcha savollarni Options bilan birga olish
            // Savollar xotiradagi keshdan (bazaga murojaat yo'q), tasodifiy 100 tasi
            var randomQuestions = (await _bank.RandomAsync(100))
               .Select(QuestionJson.From)
                .ToList();

            return Json(randomQuestions);
        }
    }
}
