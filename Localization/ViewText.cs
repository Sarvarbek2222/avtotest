using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Localization;

namespace propro.Localization
{
    /// <summary>
    /// View'larda ishlatish uchun: @L["kalit"], @L.Html("kalit"), @L.F("kalit", ("n", 5)).
    /// Standart IStringLocalizer&lt;SharedResource&gt; ustidagi yupqa qobiq (Resources/SharedResource*.resx).
    /// _ViewImports.cshtml orqali barcha view'larga inject qilingan.
    /// </summary>
    public class ViewText
    {
        private readonly IHttpContextAccessor _http;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private string? _lang;

        public ViewText(IHttpContextAccessor http, IStringLocalizer<SharedResource> localizer)
        {
            _http = http;
            _localizer = localizer;
        }

        /// <summary>Joriy til kodi: uz | ru | uzk</summary>
        public string Lang => _lang ??= Localization.Lang.Current(_http.HttpContext);

        public string HtmlLang => Localization.Lang.HtmlLang(Lang);

        public string this[string key] => _localizer[key];

        /// <summary>HTML teglar (&lt;br&gt;, &lt;span&gt;) bo'lgan ishonchli tarjima matni.</summary>
        public IHtmlContent Html(string key) => new HtmlString(_localizer[key]);

        public string F(string key, params (string name, object? value)[] args)
        {
            string s = _localizer[key];
            foreach (var (name, value) in args) s = s.Replace("{" + name + "}", value?.ToString());
            return s;
        }

        /// <summary>Tanlangan tildagi ma'lumot; tarjima yo'q bo'lsa — o'zbek (lotin).</summary>
        public string Pick(string? uz, string? ru, string? uzk) => Localization.Lang.Pick(Lang, uz, ru, uzk);

        public string Topic(string? topic) => Topics.Display(topic, Lang);

        /// <summary>Test nomi: "Bilet №5", "Mavzu bo'yicha: Svetofor ishoralari", "20 savol" ...</summary>
        public string TestName(string type, string? testRef)
        {
            string name = this["testType." + type];
            if (string.IsNullOrWhiteSpace(testRef)) return name;
            return type switch
            {
                TestTypes.Bilet => $"{name} №{testRef}",
                TestTypes.Topic => $"{name}: {Topic(testRef)}",
                _ => name,
            };
        }

        /// <summary>Tahlil tavsiyasi matni (parametrlar bilan, mavzular tanlangan tilda).</summary>
        public string InsightText(Insight insight)
        {
            var args = insight.Args.ToList();
            if (insight.Topics.Count > 0)
                args.Add(("topics", string.Join(", ", insight.Topics.Select(Topic))));
            return F(insight.Key, args.ToArray());
        }

        public string Stamp(DateTime utc) => utc.ToLocalTime().ToString("dd.MM.yyyy HH:mm");

        public string Date(DateTime local) => local.ToString("d MMMM yyyy");

        public string Duration(int minutes) => minutes < 60
            ? F("cab.minutes", ("n", minutes))
            : F("cab.hoursMinutes", ("h", minutes / 60), ("m", minutes % 60));
    }
}
