using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace propro.Controllers.Api
{
    /// <summary>
    /// Mobil ilova API'si uchun asos: JWT token bilan ishlaydi (saytning cookie'si bilan aralashmaydi).
    /// Xatolar ilovaga { "error": "kod", "message": "..." } ko'rinishida qaytadi.
    /// </summary>
    [ApiController]
    [Authorize(AuthenticationSchemes = ApiTokens.Scheme)]
    [Produces("application/json")]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected int CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        protected ObjectResult Error(int status, string code, string message) =>
            StatusCode(status, new ApiError(code, message));

        /// <summary>Obuna tugagan bo'lsa — 402 "subscription_required" (ilova to'lov sahifasini ochadi).</summary>
        protected async Task<(AppUser? user, IActionResult? fail)> RequireSubscriptionAsync(AppDbContext db)
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == CurrentUserId);
            if (user == null) return (null, Error(401, "unauthorized", "Qaytadan kiring."));
            if (user.IsExpired(DateTime.UtcNow))
                return (null, Error(402, "subscription_required", "Obuna muddati tugagan. Obunani uzaytiring."));
            return (user, null);
        }
    }

    public record ApiError(string Error, string Message);

    public record ApiUser(int Id, string Username, string? FullName, string? Phone, string Role, bool IsAdmin)
    {
        public static ApiUser From(AppUser u) => new(u.Id, u.Username, u.FullName, u.Phone, u.Role, u.Role == Roles.SuperAdmin);
    }
}
