using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.Extensions.Localization;
using System.Linq;
using System.Threading.Tasks;

namespace propro.Controllers
{
    public class BiletController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<SharedResource> _t;

        private readonly QuestionBank _bank;

        public BiletController(AppDbContext context, IStringLocalizer<SharedResource> t, QuestionBank bank)
        {
            _context = context;
            _t = t;
            _bank = bank;
        }

        public IActionResult Index() => View();

        public IActionResult TestPage(int biletNumber)
        {
            ViewData["BiletNumber"] = biletNumber;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetBiletQuestions(int biletNumber)
        {
            const int questionsPerBilet = 20;
            // Savollar xotiradagi keshdan (Id bo'yicha tartiblangan)
            var allQuestions = await _bank.AllAsync();

            var totalBilets = (int)Math.Ceiling(allQuestions.Count / (double)questionsPerBilet);

            if (biletNumber < 1 || biletNumber > totalBilets)
                return BadRequest(_t["bilet.range"].Value.Replace("{max}", totalBilets.ToString()));

            var biletQuestions = allQuestions
                .Skip((biletNumber - 1) * questionsPerBilet)
                .Take(questionsPerBilet)
                //.OrderBy(q => Guid.NewGuid())
               .Select(QuestionJson.From)
                .ToList();

            return Json(new
            {
                biletNumber,
                totalBilets,
                questions = biletQuestions
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBilets()
        {
            const int questionsPerBilet = 20;
            var totalQuestions = (await _bank.AllAsync()).Count;
            var totalBilets = (int)Math.Ceiling(totalQuestions / (double)questionsPerBilet);

            return Json(new { totalBilets });
        }
    }
}