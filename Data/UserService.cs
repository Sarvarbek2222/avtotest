using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Foydalanuvchilar bilan ishlash: parol xeshlash (ASP.NET Core PasswordHasher — PBKDF2), tekshirish,
/// kirish (cookie) va standart super adminni yaratish.
/// </summary>
public class UserService
{
    public const string StampClaim = "stamp";
    public const int MinPasswordLength = 6;

    private static readonly Regex UsernameRx = new(@"^[A-Za-z0-9._-]{3,64}$", RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly IPasswordHasher<AppUser> _hasher;

    public UserService(AppDbContext db, IPasswordHasher<AppUser> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public static bool IsValidUsername(string? username) => username != null && UsernameRx.IsMatch(username);

    public void SetPassword(AppUser user, string password)
    {
        user.PasswordHash = _hasher.HashPassword(user, password);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
    }

    /// <summary>Login/parolni tekshiradi. Nofaol foydalanuvchi uchun ham obyekt qaytadi — chaqiruvchi IsActive ni tekshiradi.</summary>
    public async Task<AppUser?> VerifyAsync(string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password)) return null;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username.Trim());
        if (user == null)
        {
            // Vaqt bo'yicha farq bilan login borligini bilib olishning oldini olish
            _hasher.HashPassword(new AppUser(), password);
            return null;
        }

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed) return null;

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, password);
            await _db.SaveChangesAsync();
        }
        return user;
    }

    public static ClaimsPrincipal CreatePrincipal(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
            new(StampClaim, user.SecurityStamp),
        };
        if (!string.IsNullOrWhiteSpace(user.FullName))
            claims.Add(new Claim(ClaimTypes.GivenName, user.FullName));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    public async Task SignInAsync(HttpContext http, AppUser user)
    {
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, CreatePrincipal(user),
            new AuthenticationProperties { IsPersistent = true });
    }

    /// <summary>
    /// Har so'rovda cookie'ni tekshiradi: foydalanuvchi o'chirilgan, nofaol qilingan yoki paroli/roli
    /// o'zgargan bo'lsa — darhol tizimdan chiqariladi.
    /// </summary>
    public static async Task ValidatePrincipalAsync(CookieValidatePrincipalContext ctx)
    {
        var idStr = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var stamp = ctx.Principal?.FindFirstValue(StampClaim);

        var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var user = int.TryParse(idStr, out var id)
            ? await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id)
            : null;

        if (user == null || !user.IsActive || user.SecurityStamp != stamp)
        {
            ctx.RejectPrincipal();
            await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }

    /// <summary>Faol super adminlar soni (oxirgisini o'chirish/pasaytirishga yo'l qo'ymaslik uchun).</summary>
    public Task<int> ActiveSuperAdminCountAsync(int? exceptUserId = null) =>
        _db.Users.CountAsync(u => u.Role == Roles.SuperAdmin && u.IsActive && u.Id != exceptUserId);

    /// <summary>Birorta ham super admin bo'lmasa — appsettings.json dagi "DefaultAdmin" bilan yaratadi.</summary>
    public async Task<AppUser?> EnsureDefaultSuperAdminAsync(IConfiguration config, ILogger log)
    {
        if (await _db.Users.AnyAsync(u => u.Role == Roles.SuperAdmin)) return null;

        var username = config["DefaultAdmin:Username"] ?? "admin";
        var password = config["DefaultAdmin:Password"] ?? "Admin@2026";

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            user = new AppUser { Username = username, FullName = "Super Admin" };
            _db.Users.Add(user);
        }
        user.Role = Roles.SuperAdmin;
        user.IsActive = true;
        SetPassword(user, password);
        await _db.SaveChangesAsync();

        log.LogWarning("Standart super admin yaratildi: login '{Username}'. Parolni admin panelida darhol o'zgartiring!", username);
        return user;
    }
}
