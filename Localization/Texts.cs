using System.Collections;
using System.Globalization;
using System.Resources;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace propro.Localization
{
    /// <summary>
    /// Tarjima fayllariga (Resources/SharedResource*.resx) statik kirish — IStringLocalizer ishlatib
    /// bo'lmaydigan joylar uchun (aniq til bo'yicha olish, brauzer uchun lug'at).
    /// Controller va view'larda standart IStringLocalizer&lt;SharedResource&gt; / @L ishlatiladi —
    /// ikkalasi ham aynan shu resx fayllarni o'qiydi.
    /// </summary>
    public static class Texts
    {
        private static readonly ResourceManager Rm =
            new("propro.Resources.SharedResource", typeof(SharedResource).Assembly);

        private static readonly Lazy<string> Json = new(BuildJson);
        private static readonly Lazy<string> VersionLazy = new(() =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Json.Value)), 0, 6).ToLowerInvariant());

        /// <summary>Lug'at mazmunidan olingan qisqa xesh — brauzer keshini yangilash uchun.</summary>
        public static string Version => VersionLazy.Value;

        /// <summary>Berilgan tildagi matn; topilmasa — o'zbek (lotin), u ham bo'lmasa kalitning o'zi.</summary>
        public static string Get(string lang, string key) =>
            Rm.GetString(key, new CultureInfo(Lang.CultureOf(lang))) ?? key;

        public static string Format(string lang, string key, IDictionary<string, object?> args)
        {
            var s = Get(lang, key);
            foreach (var (k, val) in args) s = s.Replace("{" + k + "}", val?.ToString());
            return s;
        }

        /// <summary>Brauzer uchun: {"uz":{...},"ru":{...},"uzk":{...}}</summary>
        public static string ToJson() => Json.Value;

        private static string BuildJson()
        {
            // Kalitlar ro'yxati standart (o'zbek lotin) fayldan olinadi
            var keys = new List<string>();
            var neutral = Rm.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: false)!;
            foreach (DictionaryEntry e in neutral) keys.Add((string)e.Key);
            keys.Sort(StringComparer.Ordinal);

            var result = new Dictionary<string, Dictionary<string, string>>();
            foreach (var lang in Lang.All)
            {
                var culture = new CultureInfo(Lang.CultureOf(lang));
                result[lang] = keys.ToDictionary(k => k, k => Rm.GetString(k, culture) ?? k);
            }

            return JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            });
        }
    }
}
