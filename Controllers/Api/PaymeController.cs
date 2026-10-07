using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace propro.Controllers.Api
{
    /// <summary>
    /// Payme Merchant API (JSON-RPC). Payme kabinetida "Endpoint URL" sifatida https://SAYT/api/pay/payme yoziladi,
    /// hisob (account) maydoni — "order_id". To'lov bajarilganda (PerformTransaction) obuna avtomatik faollashadi.
    /// Hujjat: https://developer.help.paycom.uz/metody-merchant-api/
    /// </summary>
    [ApiController]
    [AllowAnonymous]
    [Route("api/pay/payme")]
    public class PaymeController : ControllerBase
    {
        private const int StateCreated = 1, StatePerformed = 2, StateCancelled = -1, StateCancelledAfterPerform = -2;

        private readonly AppDbContext _db;
        private readonly SubscriptionService _subs;
        private readonly ILogger<PaymeController> _log;

        public PaymeController(AppDbContext db, SubscriptionService subs, ILogger<PaymeController> log)
        {
            _db = db;
            _subs = subs;
            _log = log;
        }

        [HttpPost]
        public async Task<IActionResult> Rpc()
        {
            JsonElement root;
            try
            {
                using var doc = await JsonDocument.ParseAsync(Request.Body);
                root = doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                return Fail(null, -32700, "Parse error");
            }

            var id = root.TryGetProperty("id", out var idEl) ? idEl.Clone() : (JsonElement?)null;
            if (!Authorized()) return Fail(id, -32504, "Insufficient privilege");

            var method = root.TryGetProperty("method", out var m) ? m.GetString() : null;
            var p = root.TryGetProperty("params", out var pe) ? pe : default;

            try
            {
                return method switch
                {
                    "CheckPerformTransaction" => await CheckPerform(id, p),
                    "CreateTransaction" => await Create(id, p),
                    "PerformTransaction" => await Perform(id, p),
                    "CancelTransaction" => await Cancel(id, p),
                    "CheckTransaction" => await Check(id, p),
                    "GetStatement" => await Statement(id, p),
                    _ => Fail(id, -32601, "Method not found"),
                };
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Payme {Method} xatosi", method);
                return Fail(id, -32400, "System error");
            }
        }

        // ── usullar ─────────────────────────────────────────────────────────────

        private async Task<IActionResult> CheckPerform(JsonElement? id, JsonElement p)
        {
            var (order, err) = await FindOrderForPayment(id, p);
            if (err != null) return err;
            return Ok(Result(id, new { allow = true }));
        }

        private async Task<IActionResult> Create(JsonElement? id, JsonElement p)
        {
            var transId = p.GetProperty("id").GetString()!;
            var time = p.GetProperty("time").GetInt64();

            var existing = await _db.Payments.FirstOrDefaultAsync(x => x.Provider == PaymentProviders.Payme && x.ProviderTransId == transId);
            if (existing != null)
            {
                if (existing.ProviderState != StateCreated) return Fail(id, -31008, "Transaction cannot be performed");
                if (Expired(existing))
                {
                    await CancelPending(existing, 4);
                    return Fail(id, -31008, "Transaction timed out");
                }
                return Ok(Result(id, new { create_time = existing.ProviderCreateTime, transaction = existing.Id.ToString(), state = existing.ProviderState }));
            }

            var (order, err) = await FindOrderForPayment(id, p);
            if (err != null) return err;

            // Buyurtmaga boshqa faol Payme tranzaksiyasi bog'langan bo'lsa — yangisini qabul qilmaymiz
            if (!string.IsNullOrEmpty(order!.ProviderTransId) && order.ProviderState == StateCreated)
                return Fail(id, -31099, "Order is awaiting another transaction", "order_id");

            order.ProviderTransId = transId;
            order.ProviderCreateTime = time;
            order.ProviderState = StateCreated;
            await _db.SaveChangesAsync();
            return Ok(Result(id, new { create_time = order.ProviderCreateTime, transaction = order.Id.ToString(), state = order.ProviderState }));
        }

        private async Task<IActionResult> Perform(JsonElement? id, JsonElement p)
        {
            var order = await ByTransId(p);
            if (order == null) return Fail(id, -31003, "Transaction not found");

            if (order.ProviderState == StateCreated)
            {
                if (Expired(order))
                {
                    await CancelPending(order, 4);
                    return Fail(id, -31008, "Transaction timed out");
                }
                order.ProviderState = StatePerformed;
                order.ProviderPerformTime = NowMs();
                await _subs.MarkPaidAsync(order); // obuna shu yerda faollashadi
            }
            else if (order.ProviderState != StatePerformed)
            {
                return Fail(id, -31008, "Transaction cannot be performed");
            }
            return Ok(Result(id, new { transaction = order.Id.ToString(), perform_time = order.ProviderPerformTime, state = order.ProviderState }));
        }

        private async Task<IActionResult> Cancel(JsonElement? id, JsonElement p)
        {
            var order = await ByTransId(p);
            if (order == null) return Fail(id, -31003, "Transaction not found");
            var reason = p.TryGetProperty("reason", out var r) && r.ValueKind == JsonValueKind.Number ? r.GetInt32() : 0;

            if (order.ProviderState == StateCreated)
            {
                await CancelPending(order, reason);
            }
            else if (order.ProviderState == StatePerformed)
            {
                order.ProviderState = StateCancelledAfterPerform;
                order.ProviderCancelTime = NowMs();
                order.CancelReason = reason;
                await _subs.RevokeAsync(order); // pul qaytarildi — obuna davri qaytarib olinadi
            }
            return Ok(Result(id, new { transaction = order.Id.ToString(), cancel_time = order.ProviderCancelTime, state = order.ProviderState }));
        }

        private async Task<IActionResult> Check(JsonElement? id, JsonElement p)
        {
            var order = await ByTransId(p);
            if (order == null) return Fail(id, -31003, "Transaction not found");
            return Ok(Result(id, new
            {
                create_time = order.ProviderCreateTime,
                perform_time = order.ProviderPerformTime,
                cancel_time = order.ProviderCancelTime,
                transaction = order.Id.ToString(),
                state = order.ProviderState,
                reason = order.CancelReason,
            }));
        }

        private async Task<IActionResult> Statement(JsonElement? id, JsonElement p)
        {
            var from = p.GetProperty("from").GetInt64();
            var to = p.GetProperty("to").GetInt64();
            var list = await _db.Payments.AsNoTracking()
                .Where(x => x.Provider == PaymentProviders.Payme && x.ProviderTransId != null &&
                            x.ProviderCreateTime >= from && x.ProviderCreateTime <= to)
                .OrderBy(x => x.ProviderCreateTime)
                .ToListAsync();

            return Ok(Result(id, new
            {
                transactions = list.Select(x => new
                {
                    id = x.ProviderTransId,
                    time = x.ProviderCreateTime,
                    amount = x.Amount * 100,
                    account = new { order_id = x.Id.ToString() },
                    create_time = x.ProviderCreateTime,
                    perform_time = x.ProviderPerformTime,
                    cancel_time = x.ProviderCancelTime,
                    transaction = x.Id.ToString(),
                    state = x.ProviderState,
                    reason = x.CancelReason,
                }),
            }));
        }

        // ── yordamchilar ────────────────────────────────────────────────────────

        /// <summary>params.account.order_id va params.amount (tiyin) bo'yicha to'lanishi mumkin bo'lgan buyurtmani topadi.</summary>
        private async Task<(Payment? order, IActionResult? error)> FindOrderForPayment(JsonElement? id, JsonElement p)
        {
            string? orderIdStr = null;
            if (p.TryGetProperty("account", out var acc) && acc.TryGetProperty("order_id", out var oid))
                orderIdStr = oid.ValueKind == JsonValueKind.Number ? oid.GetRawText() : oid.GetString();

            if (!int.TryParse(orderIdStr, out var orderId))
                return (null, Fail(id, -31050, "Order not found", "order_id"));

            var order = await _db.Payments.Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == orderId && x.Provider == PaymentProviders.Payme);
            if (order == null || order.User == null)
                return (null, Fail(id, -31050, "Order not found", "order_id"));
            if (order.Status != PaymentStatus.Pending)
                return (null, Fail(id, -31051, "Order already paid or cancelled", "order_id"));
            if (DateTime.UtcNow - order.CreatedAt > SubscriptionService.PendingLifetime)
                return (null, Fail(id, -31052, "Order expired", "order_id"));

            var amount = p.TryGetProperty("amount", out var a) ? a.GetInt64() : -1;
            if (amount != order.Amount * 100)
                return (null, Fail(id, -31001, "Incorrect amount"));

            return (order, null);
        }

        private Task<Payment?> ByTransId(JsonElement p)
        {
            var transId = p.GetProperty("id").GetString();
            return _db.Payments.Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Provider == PaymentProviders.Payme && x.ProviderTransId == transId);
        }

        private static bool Expired(Payment order) =>
            NowMs() - order.ProviderCreateTime > (long)SubscriptionService.PendingLifetime.TotalMilliseconds;

        private async Task CancelPending(Payment order, int reason)
        {
            order.ProviderState = StateCancelled;
            order.ProviderCancelTime = NowMs();
            order.CancelReason = reason;
            order.Status = PaymentStatus.Cancelled;
            await _db.SaveChangesAsync();
        }

        /// <summary>Authorization: Basic base64("Paycom:KALIT").</summary>
        private bool Authorized()
        {
            var key = _subs.Options.Payme.Key;
            if (string.IsNullOrWhiteSpace(key)) return false;
            var header = Request.Headers.Authorization.ToString();
            if (!header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)) return false;
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header[6..].Trim()));
                var expected = "Paycom:" + key;
                return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(decoded), Encoding.UTF8.GetBytes(expected));
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        private static object Result(JsonElement? id, object result) => new { jsonrpc = "2.0", id, result };

        private IActionResult Fail(JsonElement? id, int code, string en, string? data = null) => Ok(new
        {
            jsonrpc = "2.0",
            id,
            error = new
            {
                code,
                message = new { uz = Uz(code), ru = Ru(code), en },
                data,
            },
        });

        private static string Uz(int code) => code switch
        {
            -31001 => "Summa noto'g'ri",
            -31003 => "Tranzaksiya topilmadi",
            -31008 => "Amalni bajarib bo'lmaydi",
            -31050 => "Buyurtma topilmadi",
            -31051 => "Buyurtma allaqachon to'langan yoki bekor qilingan",
            -31052 => "Buyurtma muddati o'tgan",
            -31099 => "Buyurtma boshqa tranzaksiyani kutmoqda",
            -32504 => "Ruxsat yo'q",
            _ => "Tizim xatosi",
        };

        private static string Ru(int code) => code switch
        {
            -31001 => "Неверная сумма",
            -31003 => "Транзакция не найдена",
            -31008 => "Невозможно выполнить операцию",
            -31050 => "Заказ не найден",
            -31051 => "Заказ уже оплачен или отменён",
            -31052 => "Срок заказа истёк",
            -31099 => "Заказ ожидает другую транзакцию",
            -32504 => "Недостаточно привилегий",
            _ => "Системная ошибка",
        };
    }
}
