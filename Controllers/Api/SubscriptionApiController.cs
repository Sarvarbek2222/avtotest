using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace propro.Controllers.Api
{
    /// <summary>
    /// Obuna: tariflar, buyurtma yaratish (Click/Payme sahifasi manzili bilan) va to'lov holatini kuzatish.
    /// Ilova to'lov sahifasini ochadi, so'ng GET orders/{id} orqali holatni tekshiradi — to'lov tasdiqlangan
    /// zahoti obuna faol bo'ladi (Click/Payme serverimizga o'zi xabar beradi).
    /// </summary>
    [Route("api/subscription")]
    public class SubscriptionApiController : ApiControllerBase
    {
        private readonly AppDbContext _db;
        private readonly SubscriptionService _subs;

        public SubscriptionApiController(AppDbContext db, SubscriptionService subs)
        {
            _db = db;
            _subs = subs;
        }

        public record PlanDto(int Id, string Name, int Months, long Price, long PricePerMonth, string? Badge);
        public record OrderRequest(int PlanId, string? Provider);
        public record OrderDto(int Id, string Provider, string Status, long Amount, int Months, string? PlanName, DateTime CreatedAt, DateTime? PaidAt, DateTime? PeriodEnd, string? CheckoutUrl);

        [HttpGet("plans")]
        [AllowAnonymous]
        public async Task<IActionResult> Plans()
        {
            var plans = (await _subs.ActivePlansAsync())
                .Select(p => new PlanDto(p.Id, p.Name, p.Months, p.Price, p.Months > 0 ? p.Price / p.Months : p.Price, p.Badge));
            return Ok(new { plans, providers = _subs.AvailableProviders(), testMode = _subs.Options.TestMode });
        }

        [HttpGet("status")]
        public async Task<IActionResult> Status()
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == CurrentUserId);
            if (user == null) return Error(401, "unauthorized", "Qaytadan kiring.");
            return Ok(SubscriptionService.StatusOf(user, DateTime.UtcNow));
        }

        [HttpPost("orders")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderRequest req)
        {
            var order = await _subs.CreateOrderAsync(CurrentUserId, req.PlanId, req.Provider ?? "");
            if (order == null) return Error(400, "order_invalid", "Tarif yoki to'lov usuli noto'g'ri.");
            await _db.Entry(order).Reference(o => o.Plan).LoadAsync();
            return Ok(ToDto(order, _subs.CheckoutUrl(order)));
        }

        [HttpGet("orders/{id:int}")]
        public async Task<IActionResult> Order(int id)
        {
            var order = await _db.Payments.AsNoTracking().Include(p => p.Plan)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == CurrentUserId);
            if (order == null) return Error(404, "not_found", "To'lov topilmadi.");
            var user = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == CurrentUserId);
            return Ok(new { order = ToDto(order, null), subscription = SubscriptionService.StatusOf(user, DateTime.UtcNow) });
        }

        /// <summary>Faqat sinov rejimida (Payments:TestMode = true): haqiqiy pulsiz to'lovni tasdiqlaydi.</summary>
        [HttpPost("orders/{id:int}/test-pay")]
        public async Task<IActionResult> TestPay(int id)
        {
            if (!_subs.Options.TestMode) return Error(403, "test_mode_off", "Sinov rejimi o'chirilgan.");
            var order = await _db.Payments.Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == CurrentUserId && p.Provider == PaymentProviders.Test);
            if (order == null) return Error(404, "not_found", "To'lov topilmadi.");
            await _subs.MarkPaidAsync(order);
            return await Order(id);
        }

        [HttpGet("history")]
        public async Task<IActionResult> History()
        {
            var list = await _db.Payments.AsNoTracking().Include(p => p.Plan)
                .Where(p => p.UserId == CurrentUserId && p.Status == PaymentStatus.Paid)
                .OrderByDescending(p => p.PaidAt)
                .Take(50)
                .ToListAsync();
            return Ok(list.Select(p => ToDto(p, null)));
        }

        private static OrderDto ToDto(Payment p, string? checkoutUrl) =>
            new(p.Id, p.Provider, p.Status, p.Amount, p.Months, p.Plan?.Name, p.CreatedAt, p.PaidAt, p.PeriodEnd, checkoutUrl);
    }
}
