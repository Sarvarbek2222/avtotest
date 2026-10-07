using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace propro.Controllers.Api
{
    /// <summary>
    /// Click SHOP API. Click kabinetida: Prepare URL — https://SAYT/api/pay/click/prepare,
    /// Complete URL — https://SAYT/api/pay/click/complete. merchant_trans_id = buyurtma raqami (Payments.Id).
    /// Complete muvaffaqiyatli bo'lganda obuna avtomatik faollashadi.
    /// Hujjat: https://docs.click.uz/click-api-request/
    /// </summary>
    [ApiController]
    [AllowAnonymous]
    [Route("api/pay/click")]
    public class ClickController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly SubscriptionService _subs;

        public ClickController(AppDbContext db, SubscriptionService subs)
        {
            _db = db;
            _subs = subs;
        }

        public class ClickRequest
        {
            public long click_trans_id { get; set; }
            public long service_id { get; set; }
            public long click_paydoc_id { get; set; }
            public string? merchant_trans_id { get; set; }
            public long? merchant_prepare_id { get; set; }
            public string? amount { get; set; }
            public int action { get; set; }
            public int error { get; set; }
            public string? error_note { get; set; }
            public string? sign_time { get; set; }
            public string? sign_string { get; set; }
        }

        [HttpPost("prepare")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> Prepare([FromForm] ClickRequest r)
        {
            if (!SignOk(r, withPrepareId: false)) return Reply(r, -1, "SIGN CHECK FAILED!");
            if (r.action != 0) return Reply(r, -3, "Action not found");

            var (order, err) = await FindOrder(r);
            if (err != null) return err;

            if (order!.Status == PaymentStatus.Paid) return Reply(r, -4, "Already paid");
            if (order.Status == PaymentStatus.Cancelled) return Reply(r, -9, "Transaction cancelled");

            order.ProviderTransId = r.click_trans_id.ToString();
            order.ProviderState = 1;
            await _db.SaveChangesAsync();
            return Reply(r, 0, "Success", prepareId: order.Id);
        }

        [HttpPost("complete")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> Complete([FromForm] ClickRequest r)
        {
            if (!SignOk(r, withPrepareId: true)) return Reply(r, -1, "SIGN CHECK FAILED!");
            if (r.action != 1) return Reply(r, -3, "Action not found");

            var (order, err) = await FindOrder(r);
            if (err != null) return err;
            if (r.merchant_prepare_id != order!.Id) return Reply(r, -6, "Transaction does not exist");
            if (order.ProviderTransId != r.click_trans_id.ToString()) return Reply(r, -6, "Transaction does not exist");

            if (order.Status == PaymentStatus.Paid) return Reply(r, -4, "Already paid", confirmId: order.Id);
            if (order.Status == PaymentStatus.Cancelled) return Reply(r, -9, "Transaction cancelled");

            if (r.error < 0)
            {
                // Click to'lovni amalga oshira olmadi (karta rad etdi va h.k.)
                order.Status = PaymentStatus.Cancelled;
                order.ProviderState = -1;
                await _db.SaveChangesAsync();
                return Reply(r, -9, "Transaction cancelled");
            }

            order.ProviderState = 2;
            await _subs.MarkPaidAsync(order); // obuna shu yerda faollashadi
            return Reply(r, 0, "Success", confirmId: order.Id);
        }

        private async Task<(Payment? order, IActionResult? error)> FindOrder(ClickRequest r)
        {
            if (!int.TryParse(r.merchant_trans_id, out var orderId)) return (null, Reply(r, -5, "User does not exist"));
            var order = await _db.Payments.Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == orderId && p.Provider == PaymentProviders.Click);
            if (order?.User == null) return (null, Reply(r, -5, "User does not exist"));

            if (!decimal.TryParse(r.amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount != order.Amount)
                return (null, Reply(r, -2, "Incorrect parameter amount"));
            return (order, null);
        }

        /// <summary>md5(click_trans_id + service_id + SECRET_KEY + merchant_trans_id + [merchant_prepare_id] + amount + action + sign_time)</summary>
        private bool SignOk(ClickRequest r, bool withPrepareId)
        {
            var o = _subs.Options.Click;
            if (!o.Enabled || r.sign_string == null) return false;
            if (r.service_id.ToString() != o.ServiceId) return false;

            var raw = $"{r.click_trans_id}{r.service_id}{o.SecretKey}{r.merchant_trans_id}" +
                      (withPrepareId ? r.merchant_prepare_id?.ToString() : "") +
                      $"{r.amount}{r.action}{r.sign_time}";
            var hash = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
            return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(hash), Encoding.ASCII.GetBytes(r.sign_string.ToLowerInvariant()));
        }

        private IActionResult Reply(ClickRequest r, int error, string note, int? prepareId = null, int? confirmId = null) => Ok(new
        {
            click_trans_id = r.click_trans_id,
            merchant_trans_id = r.merchant_trans_id,
            merchant_prepare_id = prepareId,
            merchant_confirm_id = confirmId,
            error,
            error_note = note,
        });
    }

    /// <summary>To'lovdan keyin brauzer qaytadigan sahifa (Payments:ReturnUrl = https://SAYT/api/pay/done).</summary>
    [AllowAnonymous]
    [Route("api/pay/done")]
    public class PaymentDoneController : Controller
    {
        [HttpGet]
        public ContentResult Done() => Content(
            "<!doctype html><html lang=\"uz\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">" +
            "<title>AVTOPRAVA_SAM</title></head><body style=\"margin:0;font-family:system-ui,sans-serif;background:#07080d;color:#f1f2f6;" +
            "display:grid;place-items:center;min-height:100vh;text-align:center;padding:24px;box-sizing:border-box\"><div>" +
            "<h1 style=\"font-size:22px\">To'lov yuborildi</h1><p style=\"color:#9aa0b8\">Ilovaga qayting — obuna bir necha soniyada faollashadi.</p>" +
            "</div></body></html>", "text/html; charset=utf-8");
    }
}
