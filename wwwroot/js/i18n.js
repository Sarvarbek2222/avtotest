/*
 * Til tizimi (uz — o'zbek lotin, ru — rus, uzk — o'zbek kirill).
 *
 * Lug'at /Lang/Texts dan yuklanadi (window.I18N_TEXTS) — manbasi Resources/SharedResource*.resx.
 * Tanlangan til ASP.NET Core'ning standart ".AspNetCore.Culture" cookie'sida 1 yil saqlanadi —
 * server ham sahifani shu tilda chizadi. Til almashtirilganda sahifa qayta yuklanmaydi:
 *   [data-i18n]             → textContent
 *   [data-i18n-html]        → innerHTML (faqat lug'atdagi ishonchli matn)
 *   [data-i18n-placeholder] → placeholder
 *   [data-i18n-title]       → title
 *   [data-i18n-args='{"n":5}'] → {n} parametrlari
 * va document'da "i18n:change" hodisasi chiqariladi (dinamik matnlarni qayta chizish uchun).
 *
 * API: I18n.lang, I18n.t(key, args), I18n.pick(uz, ru, uzk), I18n.setLang(lang), I18n.apply(root), I18n.toCyrillic(text)
 */
(function () {
    "use strict";

    var COOKIE = ".AspNetCore.Culture";
    var LANGS = ["uz", "ru", "uzk"];
    var HTML_LANG = { uz: "uz", ru: "ru", uzk: "uz-Cyrl" };
    var CULTURE = { uz: "uz-Latn", ru: "ru", uzk: "uz-Cyrl" };

    function normalize(l) {
        l = (l || "").toString().trim().toLowerCase();
        if (l === "ru") return "ru";
        if (l === "uzk" || l === "en" || l === "uz-cyrl" || l === "cyrl") return "uzk"; // "en" — eski kirill kodi
        return "uz";
    }

    function writeCookie(l) {
        // Format: c=uz-Cyrl|uic=uz-Cyrl (CookieRequestCultureProvider)
        var c = CULTURE[l];
        document.cookie = COOKIE + "=" + encodeURIComponent("c=" + c + "|uic=" + c) +
            "; path=/; max-age=31536000; SameSite=Lax";
        // Eski versiyaning "lang" cookie'sini o'chiramiz
        document.cookie = "lang=; path=/; max-age=0";
    }

    var texts = window.I18N_TEXTS || {};
    // Joriy tilni server aniqlaydi va <html data-lang="..."> ga yozadi
    var lang = normalize(document.documentElement.getAttribute("data-lang"));

    function format(s, args) {
        if (!args) return s;
        return s.replace(/\{(\w+)\}/g, function (m, k) { return args[k] !== undefined ? args[k] : m; });
    }

    function t(key, args) {
        var d = texts[lang] || {};
        var s = d[key];
        if (s === undefined) s = (texts.uz || {})[key];
        if (s === undefined) s = key;
        return format(s, args);
    }

    /** Ma'lumot matni: tanlangan tilda, bo'sh bo'lsa — o'zbek (lotin). */
    function pick(uz, ru, uzk) {
        var v = lang === "ru" ? ru : lang === "uzk" ? uzk : uz;
        return v && String(v).trim() ? v : (uz || "");
    }

    function argsOf(el) {
        var a = el.getAttribute("data-i18n-args");
        if (!a) return null;
        try { return JSON.parse(a); } catch (e) { return null; }
    }

    function apply(root) {
        root = root || document;
        root.querySelectorAll("[data-i18n]").forEach(function (el) {
            el.textContent = t(el.getAttribute("data-i18n"), argsOf(el));
        });
        root.querySelectorAll("[data-i18n-html]").forEach(function (el) {
            el.innerHTML = t(el.getAttribute("data-i18n-html"), argsOf(el));
        });
        root.querySelectorAll("[data-i18n-placeholder]").forEach(function (el) {
            el.setAttribute("placeholder", t(el.getAttribute("data-i18n-placeholder")));
        });
        root.querySelectorAll("[data-i18n-title]").forEach(function (el) {
            el.setAttribute("title", t(el.getAttribute("data-i18n-title")));
        });
        root.querySelectorAll("[data-i18n-alt]").forEach(function (el) {
            el.setAttribute("alt", t(el.getAttribute("data-i18n-alt")));
        });
        // Mavzu nomlari va shu kabi 3 variantli qiymatlar: data-uz / data-ru / data-uzk
        root.querySelectorAll("[data-i18n-pick]").forEach(function (el) {
            el.textContent = pick(el.getAttribute("data-uz"), el.getAttribute("data-ru"), el.getAttribute("data-uzk"));
        });

        if (root === document) {
            document.documentElement.setAttribute("lang", HTML_LANG[lang]);
            document.documentElement.setAttribute("data-lang", lang);
            var titleKey = document.documentElement.getAttribute("data-title-key");
            if (titleKey) {
                var suffix = document.documentElement.getAttribute("data-title-suffix") || "";
                document.title = t(titleKey) + suffix;
            }
        }

        document.querySelectorAll("[data-set-lang]").forEach(function (btn) {
            var on = normalize(btn.getAttribute("data-set-lang")) === lang;
            btn.classList.toggle("active", on);
            if (btn.hasAttribute("aria-checked")) btn.setAttribute("aria-checked", on ? "true" : "false");
        });
    }

    function setLang(l) {
        l = normalize(l);
        lang = l;
        writeCookie(l);
        // Serverda hisoblanadigan matnlar ko'p bo'lgan sahifalar (kabinet, tahlil) — qayta yuklanadi
        if (document.documentElement.hasAttribute("data-lang-reload")) {
            location.reload();
            return;
        }
        apply(document);
        document.dispatchEvent(new CustomEvent("i18n:change", { detail: { lang: l } }));
    }

    // Til tugmalari: <a data-set-lang="ru" href="/Lang/Set?lang=ru&returnUrl=..."> — JS bo'lsa qayta yuklanmaydi
    document.addEventListener("click", function (e) {
        var btn = e.target.closest ? e.target.closest("[data-set-lang]") : null;
        if (!btn) return;
        e.preventDefault();
        setLang(btn.getAttribute("data-set-lang"));
    });

    /* ===== O'zbek lotin → kirill (Localization/Translit.cs bilan bir xil qoidalar) ===== */

    var APOS = "'`ʻʼ‘’´";
    var SINGLE = {
        a: "а", b: "б", c: "с", d: "д", f: "ф", g: "г", h: "ҳ", i: "и", j: "ж", k: "к", l: "л", m: "м",
        n: "н", o: "о", p: "п", q: "қ", r: "р", s: "с", t: "т", u: "у", v: "в", w: "в", x: "х", y: "й", z: "з"
    };

    function isApos(c) { return !!c && APOS.indexOf(c) >= 0; }
    function isLetter(c) { return !!c && /[a-zA-Z]/.test(c); }
    function isVowel(c) { return !!c && "aeiouAEIOU".indexOf(c) >= 0; }
    function isUpper(c) { return !!c && c !== c.toLowerCase() && c === c.toUpperCase(); }
    function startsWithCi(text, pos, s) { return text.substr(pos, s.length).toLowerCase() === s; }

    function toCyrillic(text) {
        if (!text) return text || "";
        var out = "";
        var n = text.length;

        for (var i = 0; i < n; i++) {
            var c = text[i];

            if (!isLetter(c)) {
                if (isApos(c) && i > 0 && isLetter(text[i - 1]) && i + 1 < n && isLetter(text[i + 1])) {
                    var p = text[i - 1].toLowerCase();
                    if ((p === "s" || p === "c") && text[i + 1].toLowerCase() === "h") continue; // s'h → сҳ
                    out += isUpper(text[i - 1]) && isUpper(text[i + 1]) ? "Ъ" : "ъ";
                    continue;
                }
                out += c;
                continue;
            }

            var lc = c.toLowerCase();
            var upper = isUpper(c);
            var next = i + 1 < n ? text[i + 1] : "";
            var nl = next.toLowerCase();
            var nextUpper = isUpper(next);
            var wordStart = i === 0 || (!isLetter(text[i - 1]) && !isApos(text[i - 1]));
            var res, consumed = 1;

            if ((lc === "o" || lc === "g") && isApos(next)) {
                res = lc === "o" ? "ў" : "ғ"; consumed = 2;
            } else if (lc === "s" && nl === "h") {
                res = "ш"; consumed = 2;
            } else if (lc === "c" && nl === "h") {
                res = "ч"; consumed = 2;
            } else if (lc === "y" && "ouae".indexOf(nl) >= 0 && nl !== "" && !isApos(text[i + 2])) {
                res = { o: "ё", u: "ю", a: "я", e: "е" }[nl]; consumed = 2;
            } else if (lc === "t" && nl === "s" &&
                ["iya", "iyal", "ion", "io"].some(function (s) { return startsWithCi(text, i + 2, s); })) {
                res = "ц"; consumed = 2;
            } else if (lc === "e") {
                res = wordStart || (i > 0 && isVowel(text[i - 1])) ? "э" : "е";
            } else {
                res = SINGLE[lc];
            }

            if (upper || (consumed === 2 && nextUpper)) res = res.toUpperCase();
            out += res;
            i += consumed - 1;
        }
        return out;
    }

    window.I18n = {
        get lang() { return lang; },
        langs: LANGS,
        t: t,
        pick: pick,
        apply: apply,
        setLang: setLang,
        normalize: normalize,
        toCyrillic: toCyrillic
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", function () { apply(document); });
    } else {
        apply(document);
    }
})();
