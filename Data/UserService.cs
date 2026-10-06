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
    public const string DeviceClaim = "did";
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

    public static ClaimsPrincipal CreatePrincipal(AppUser user, string? deviceId = null)
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
        if (!string.IsNullOrEmpty(deviceId))
            claims.Add(new Claim(DeviceClaim, deviceId));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    public async Task SignInAsync(HttpContext http, AppUser user)
    {
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            CreatePrincipal(user, DeviceGuard.CurrentDeviceId(http)),
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

        // Muddat tugagan yoki foydalanuvchi boshqa qurilmaga o'tkazilgan (admin tasdiqlagan) — bu sessiya yopiladi
        bool expired = user != null && user.IsExpired(DateTime.UtcNow);
        bool otherDevice = user != null && user.IsRestricted && user.DeviceId != null &&
                           ctx.Principal?.FindFirstValue(DeviceClaim) != user.DeviceId;

        if (user == null || !user.IsActive || user.SecurityStamp != stamp || expired || otherDevice)
        {
            ctx.RejectPrincipal();
            await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }

    /// <summary>
    /// Parol to'g'ri bo'lgandan keyingi tekshiruvlar: faollik, kirish muddati va bitta qurilma qoidasi.
    /// Qurilma hali bog'lanmagan bo'lsa — shu qurilmaga bog'lanadi. Boshqa qurilma bo'lsa — adminga so'rov
    /// yuboriladi (bir qurilma uchun bitta kutilayotgan so'rov) va kirishga ruxsat berilmaydi.
    /// </summary>
    public async Task<LoginCheck> CheckAccessAsync(AppUser user, HttpContext http)
    {
        if (!user.IsActive) return LoginCheck.Inactive;
        if (user.IsExpired(DateTime.UtcNow)) return LoginCheck.Expired;
        if (!user.IsRestricted) return LoginCheck.Ok;

        var deviceId = DeviceGuard.GetOrCreateDeviceId(http);
        var info = DeviceGuard.Describe(http.Request.Headers.UserAgent.ToString());

        if (user.DeviceId == null)
        {
            user.DeviceId = deviceId;
            user.DeviceInfo = info;
            user.DeviceBoundAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return LoginCheck.Ok;
        }
        if (user.DeviceId == deviceId) return LoginCheck.Ok;

        var pending = await _db.DeviceRequests.FirstOrDefaultAsync(r =>
            r.UserId == user.Id && r.DeviceId == deviceId && r.Status == DeviceRequestStatus.Pending);
        if (pending == null)
        {
            _db.DeviceRequests.Add(new DeviceRequest
            {
                UserId = user.Id,
                DeviceId = deviceId,
                DeviceInfo = info,
                IpAddress = DeviceGuard.ClientIp(http),
            });
        }
        else
        {
            pending.CreatedAt = DateTime.UtcNow; // qayta urinish — so'rov ro'yxat boshiga chiqadi
            pending.IpAddress = DeviceGuard.ClientIp(http);
        }
        await _db.SaveChangesAsync();
        return LoginCheck.DevicePending;
    }

    /// <summary>Admin so'rovni tasdiqladi: foydalanuvchi yangi qurilmaga bog'lanadi, eski qurilmadagi sessiya yopiladi.</summary>
    public async Task<bool> ApproveDeviceAsync(int requestId)
    {
        var req = await _db.DeviceRequests.Include(r => r.User).FirstOrDefaultAsync(r => r.Id == requestId);
        if (req?.User == null || req.Status != DeviceRequestStatus.Pending) return false;

        var now = DateTime.UtcNow;
        req.User.DeviceId = req.DeviceId;
        req.User.DeviceInfo = req.DeviceInfo;
        req.User.DeviceBoundAt = now;
        req.Status = DeviceRequestStatus.Approved;
        req.ResolvedAt = now;

        // Shu foydalanuvchining boshqa kutilayotgan so'rovlari endi eskirdi
        var others = await _db.DeviceRequests
            .Where(r => r.UserId == req.UserId && r.Id != req.Id && r.Status == DeviceRequestStatus.Pending)
            .ToListAsync();
        foreach (var o in others)
        {
            o.Status = DeviceRequestStatus.Rejected;
            o.ResolvedAt = now;
        }
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectDeviceAsync(int requestId)
    {
        var req = await _db.DeviceRequests.FirstOrDefaultAsync(r => r.Id == requestId);
        if (req == null || req.Status != DeviceRequestStatus.Pending) return false;
        req.Status = DeviceRequestStatus.Rejected;
        req.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public Task<int> PendingDeviceRequestCountAsync() =>
        _db.DeviceRequests.CountAsync(r => r.Status == DeviceRequestStatus.Pending);

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

public enum LoginCheck
{
    Ok,
    Inactive,
    Expired,
    DevicePending,
}
