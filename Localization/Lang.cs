using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace propro.Localization
{
    /// <summary>
    /// Qo'llab-quvvatlanadigan tillar. Til ASP.NET Core RequestLocalization orqali aniqlanadi
    /// (standart ".AspNetCore.Culture" cookie'si) — bu yerdagi qisqa kodlar (uz / ru / uzk) faqat JS va
    /// ma'lumot ustunlari (QuestionUZ / QuestionRU / QuestionUZK) bilan ishlash uchun.
    /// </summary>
    public static class Lang
    {
        public const string Uz = "uz";    // O'zbek (lotin) — standart
        public const string Ru = "ru";    // Rus
        public const string Uzk = "uzk";  // O'zbek (kirill)

        public static readonly string[] All = { Uz, Ru, Uzk };

        // Tilga mos .NET madaniyatlari (resx fayl nomlari: SharedResource.resx / .ru.resx / .uz-Cyrl.resx)
        public const string UzCulture = "uz-Latn";
        public const string RuCulture = "ru";
        public const string UzkCulture = "uz-Cyrl";

        public static readonly CultureInfo[] SupportedCultures =
        {
            new(UzCulture), new(RuCulture), new(UzkCulture),
        };

        /// <summary>Oldingi versiyadagi til cookie'si — faqat o'qiladi (eski tanlov yo'qolmasin).</summary>
        public const string LegacyCookieName = "lang";

        /// <summary>Har qanday qiymatni til kodiga keltiradi. Eski "en" kodi kirillni bildirgan.</summary>
        public static string Normalize(string? value) => value?.Trim().ToLowerInvariant() switch
        {
            "ru" or "ru-ru" => Ru,
            "uzk" or "en" or "uz-cyrl" or "cyrl" => Uzk,
            _ => Uz,
        };

        public static string CultureOf(string lang) => Normalize(lang) switch
        {
            Ru => RuCulture,
            Uzk => UzkCulture,
            _ => UzCulture,
        };

        public static string FromCulture(CultureInfo culture) =>
            culture.Name.StartsWith("ru", StringComparison.OrdinalIgnoreCase) ? Ru
            : culture.Name.Equals(UzkCulture, StringComparison.OrdinalIgnoreCase) ? Uzk
            : Uz;

        /// <summary>Joriy so'rov tili (RequestLocalization middleware o'rnatgan madaniyatdan).</summary>
        public static string Current(HttpContext? ctx)
        {
            var feature = ctx?.Features.Get<IRequestCultureFeature>();
            return FromCulture(feature?.RequestCulture.UICulture ?? CultureInfo.CurrentUICulture);
        }

        /// <summary>Tanlangan tildagi matn; bo'sh bo'lsa o'zbek (lotin) varianti qaytariladi.</summary>
        public static string Pick(string lang, string? uz, string? ru, string? uzk)
        {
            var value = lang switch { Ru => ru, Uzk => uzk, _ => uz };
            return string.IsNullOrWhiteSpace(value) ? (uz ?? "") : value;
        }

        public static string HtmlLang(string lang) => lang switch
        {
            Ru => "ru",
            Uzk => "uz-Cyrl",
            _ => "uz",
        };

        /// <summary>Tanlangan tilni standart cookie'ga 1 yilga yozadi.</summary>
        public static void SetCookie(HttpResponse response, string lang)
        {
            var culture = CultureOf(lang);
            response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    Path = "/",
                    SameSite = SameSiteMode.Lax,
                    HttpOnly = false, // JS ham yozadi (sahifani qayta yuklamasdan til almashtirish uchun)
                    IsEssential = true,
                });
        }

        /// <summary>RequestLocalization sozlamalari: standart — o'zbek (lotin).</summary>
        public static void Configure(RequestLocalizationOptions options)
        {
            options.DefaultRequestCulture = new RequestCulture(UzCulture);
            options.SupportedCultures = SupportedCultures;
            options.SupportedUICultures = SupportedCultures;
            options.FallBackToParentUICultures = true;

            // Tartib: ?culture=ru → .AspNetCore.Culture cookie → eski "lang" cookie. Brauzer tili hisobga
            // olinmaydi — birinchi kirishda har doim o'zbek (lotin).
            options.RequestCultureProviders = new List<IRequestCultureProvider>
            {
                new QueryStringRequestCultureProvider(),
                new CookieRequestCultureProvider(),
                new CustomRequestCultureProvider(ctx =>
                {
                    var legacy = ctx.Request.Cookies[LegacyCookieName];
                    return Task.FromResult(legacy == null
                        ? null
                        : new ProviderCultureResult(CultureOf(legacy)));
                }),
            };
        }
    }
}
