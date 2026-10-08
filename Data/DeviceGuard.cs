using System.Text.RegularExpressions;

/// <summary>
/// Qurilmani aniqlash: har bir brauzerga bir marta tasodifiy identifikator beriladi va uzoq muddatli
/// cookie'da saqlanadi. Foydalanuvchi shu identifikatorga bog'lanadi — boshqa brauzer/qurilmadan kirish
/// uchun admin tasdig'i kerak bo'ladi.
/// </summary>
public static class DeviceGuard
{
    public const string CookieName = "propro.did";

    /// <summary>Brauzerlar cookie'ni ko'pi bilan 400 kun saqlaydi — muddat har bir sahifa ochilganda yangilanadi.</summary>
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(395);

    private static readonly Regex IdRx = new("^[a-f0-9]{32}$", RegexOptions.Compiled);

    /// <summary>Brauzerdagi qurilma identifikatori; bo'lmasa — yangisi yaratilib cookie'ga yoziladi.</summary>
    public static string GetOrCreateDeviceId(HttpContext http)
    {
        var id = http.Request.Cookies[CookieName];
        if (id != null && IdRx.IsMatch(id)) return id;

        id = Guid.NewGuid().ToString("N");
        Write(http, id);
        // Shu so'rov davomida ham bir xil qiymat o'qilsin
        http.Items[CookieName] = id;
        return id;
    }

    private static void Write(HttpContext http, string id) =>
        http.Response.Cookies.Append(CookieName, id, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = http.Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.Add(Lifetime),
        });

    /// <summary>
    /// Mavjud qurilma identifikatorining muddatini yangilaydi (sahifa so'rovlarida). Aks holda ~400 kundan keyin
    /// brauzer uni o'chirib, kompyuter "yangi qurilma" bo'lib qolardi va admin tasdig'isiz kira olmasdi.
    /// </summary>
    public static void Refresh(HttpContext http)
    {
        var id = http.Request.Cookies[CookieName];
        if (id != null && IdRx.IsMatch(id)) Write(http, id);
    }

    public static string? CurrentDeviceId(HttpContext http)
    {
        if (http.Items.TryGetValue(CookieName, out var v) && v is string s) return s;
        var id = http.Request.Cookies[CookieName];
        return id != null && IdRx.IsMatch(id) ? id : null;
    }

    /// <summary>Qurilma haqida qisqa, odam o'qiy oladigan ma'lumot: "Chrome · Windows".</summary>
    public static string Describe(string? ua)
    {
        if (string.IsNullOrWhiteSpace(ua)) return "—";

        string os =
            ua.Contains("iPhone") ? "iPhone" :
            ua.Contains("iPad") ? "iPad" :
            ua.Contains("Android") ? "Android" :
            ua.Contains("Windows") ? "Windows" :
            ua.Contains("Mac OS X") || ua.Contains("Macintosh") ? "macOS" :
            ua.Contains("CrOS") ? "ChromeOS" :
            ua.Contains("Linux") ? "Linux" : "";

        string browser =
            ua.Contains("YaBrowser/") ? "Yandex" :
            ua.Contains("Edg/") || ua.Contains("EdgA/") || ua.Contains("EdgiOS/") ? "Edge" :
            ua.Contains("OPR/") || ua.Contains("Opera") ? "Opera" :
            ua.Contains("SamsungBrowser/") ? "Samsung" :
            ua.Contains("Firefox/") || ua.Contains("FxiOS/") ? "Firefox" :
            ua.Contains("Chrome/") || ua.Contains("CriOS/") ? "Chrome" :
            ua.Contains("Safari/") ? "Safari" :
            ua.Contains("curl/") ? "curl" : "Brauzer";

        return os == "" ? browser : $"{browser} · {os}";
    }

    /// <summary>Faqat ko'rsatish uchun (xavfsizlik qarori bunga tayanmaydi): proksi orqasida X-Forwarded-For.</summary>
    public static string? ClientIp(HttpContext http)
    {
        var fwd = http.Request.Headers["X-Forwarded-For"].ToString();
        var ip = !string.IsNullOrWhiteSpace(fwd) ? fwd.Split(',')[0].Trim() : http.Connection.RemoteIpAddress?.ToString();
        return ip == null ? null : ip.Length > 64 ? ip[..64] : ip;
    }
}
