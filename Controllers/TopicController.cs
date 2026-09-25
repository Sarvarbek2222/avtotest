using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace propro.Controllers
{
    public class TopicController : Controller
    {
        private readonly AppDbContext _context;

        private readonly QuestionBank _bank;

        public TopicController(AppDbContext context, QuestionBank bank)
        {
            _context = context;
            _bank = bank;
        }

        // =========================
        // 1. MAVZULAR RO'YXATI
        // =========================
        public async Task<IActionResult> Index()
        {
            var countsRaw = await _context.Questions
                .Where(q => !string.IsNullOrWhiteSpace(q.Topic))
                .Select(q => q.Topic!.Trim())
                .GroupBy(t => t)
                .Select(g => new
                {
                    Topic = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            ViewBag.Counts = countsRaw
                .ToDictionary(x => x.Topic, x => x.Count);

            return View(Topics.All);
        }

        // =========================
        // 2. TEST PAGE
        // =========================
        public IActionResult Test(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
                return RedirectToAction(nameof(Index));

            topic = topic.Trim();

            if (!Topics.All.Contains(topic))
                return RedirectToAction(nameof(Index));

            ViewData["Topic"] = topic;

            return View();
        }

        // =========================
        // 3. QUESTION API
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetQuestions(string topic)
        {
            try
            {
                // Topic bo'sh bo'lsa
                if (string.IsNullOrWhiteSpace(topic))
                {
                    return Json(new List<object>());
                }

                topic = topic.Trim();

                // Savollar xotiradagi keshdan
                var allQuestions = await _bank.AllAsync();

                // Topic bo'yicha filter
                var filteredQuestions = allQuestions
                    .Where(q =>
                        !string.IsNullOrWhiteSpace(q.Topic) &&
                        q.Topic.Trim().Equals(
                            topic,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    .ToList();

                // JSON uchun tayyorlash
                var randomQuestions = filteredQuestions
                    .OrderBy(q => Guid.NewGuid())
                    .Select(QuestionJson.From)
                    .ToList();

                return Json(randomQuestions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = true,
                    message = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }
    }
}