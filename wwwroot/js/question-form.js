/*
 * Savol qo'shish / tahrirlash formasi (Views/Question/Create.cshtml, Edit.cshtml).
 *
 *  • Javob variantlari: qo'shish, o'chirish, to'g'ri javobni belgilash.
 *    Yuborishdan oldin nomlar qayta raqamlanadi (Options[0..n]) — o'chirilgan variant "teshik" qoldirmaydi.
 *  • Kirill maydonlari: lotin matni yozilayotganda, kirill maydoni bo'sh bo'lsa (yoki avval avtomatik
 *    to'ldirilgan bo'lsa) — avtomatik o'giriladi. Admin kirill maydonini o'zi tahrirlasa, avtomatik
 *    yangilanish to'xtaydi. "Lotindan o'girish" tugmasi — qayta o'girish.
 *
 * Bog'lanish: lotin maydon [data-tl-src="K"], kirill maydon [data-tl-dst="K"], tugma [data-tl-btn="K"].
 * Boshlang'ich ma'lumot: window.QF = { options: [{uz, ru, uzk, correct}], texts: {...} }
 */
(function () {
    "use strict";

    var t = function (k, a) { return window.I18n ? I18n.t(k, a) : k; };
    var toCyr = function (s) { return window.I18n ? I18n.toCyrillic(s) : s; };
    var LABELS = ["A", "B", "C", "D", "E", "F", "G", "H"];
    var keySeq = 0;

    function esc(s) {
        return String(s == null ? "" : s)
            .replace(/&/g, "&amp;").replace(/"/g, "&quot;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
    }

    /* ── Transliteratsiya ── */

    function pair(key) {
        return {
            src: document.querySelector('[data-tl-src="' + key + '"]'),
            dst: document.querySelector('[data-tl-dst="' + key + '"]')
        };
    }

    function flash(el) {
        el.classList.remove("tl-flash");
        void el.offsetWidth; // animatsiyani qayta boshlash
        el.classList.add("tl-flash");
    }

    function fill(dst, value) {
        dst.value = value;
        dst.dataset.auto = "1";
        dst.classList.add("tl-auto");
        flash(dst);
    }

    function bindPair(key) {
        var p = pair(key);
        if (!p.src || !p.dst) return;

        // Bo'sh kirill maydoni — avtomatik rejimda
        if (!p.dst.value.trim()) p.dst.dataset.auto = "1";

        p.src.addEventListener("input", function () {
            if (p.dst.dataset.auto === "1") {
                p.dst.value = toCyr(p.src.value);
                p.dst.classList.toggle("tl-auto", !!p.dst.value);
            }
        });

        p.dst.addEventListener("input", function () {
            // Admin qo'lda tuzatdi — endi lotin o'zgarsa ham ustidan yozmaymiz.
            // Maydonni butunlay tozalasa — yana avtomatik rejimga qaytadi.
            p.dst.dataset.auto = p.dst.value.trim() ? "0" : "1";
            p.dst.classList.remove("tl-auto");
        });
    }

    document.addEventListener("click", function (e) {
        var btn = e.target.closest("[data-tl-btn]");
        if (!btn) return;
        e.preventDefault();

        var p = pair(btn.getAttribute("data-tl-btn"));
        if (!p.src || !p.dst) return;

        var latin = p.src.value.trim();
        if (!latin) {
            alert(t("form.translitEmpty"));
            p.src.focus();
            return;
        }

        var result = toCyr(p.src.value);
        var current = p.dst.value.trim();
        if (current && current !== result.trim() && p.dst.dataset.auto !== "1" &&
            !confirm(t("form.translitOverwrite"))) {
            return;
        }
        fill(p.dst, result);
        p.dst.focus();
    });

    function translitAllEmpty() {
        var filled = 0;
        document.querySelectorAll("[data-tl-dst]").forEach(function (dst) {
            var src = document.querySelector('[data-tl-src="' + dst.getAttribute("data-tl-dst") + '"]');
            if (src && src.value.trim() && !dst.value.trim()) {
                fill(dst, toCyr(src.value));
                filled++;
            }
        });
        var note = document.getElementById("translitNote");
        if (note && filled > 0) {
            note.textContent = t("form.translitDone");
            note.style.display = "block";
        }
    }

    /* ── Javob variantlari ── */

    var list = document.getElementById("optionList");

    function renumber() {
        list.querySelectorAll(".option-item").forEach(function (item, i) {
            item.querySelector(".option-badge").textContent = LABELS[i] || (i + 1);
            item.querySelectorAll("[data-field]").forEach(function (inp) {
                inp.name = "Options[" + i + "]." + inp.getAttribute("data-field");
            });
            item.querySelector('input[type="radio"]').value = i;
        });
    }

    function markCorrect(item) {
        list.querySelectorAll(".option-item").forEach(function (el) { el.classList.remove("is-correct"); });
        if (item) item.classList.add("is-correct");
    }

    function addOption(o) {
        o = o || {};
        var key = "opt" + (++keySeq);
        var item = document.createElement("div");
        item.className = "option-item" + (o.correct ? " is-correct" : "");

        item.innerHTML =
            '<span class="option-badge"></span>' +
            '<input data-field="OptionUZ" data-tl-src="' + key + '" class="field-input" value="' + esc(o.uz) + '"' +
            ' placeholder="' + esc(t("form.langUz")) + '" data-i18n-placeholder="form.langUz" />' +
            '<input data-field="OptionRU" class="field-input" value="' + esc(o.ru) + '"' +
            ' placeholder="' + esc(t("form.langRu")) + '" data-i18n-placeholder="form.langRu" />' +
            '<div class="tl-field">' +
            '  <input data-field="OptionUZK" data-tl-dst="' + key + '" class="field-input" value="' + esc(o.uzk) + '"' +
            '   placeholder="' + esc(t("form.langUzk")) + '" data-i18n-placeholder="form.langUzk" />' +
            '  <button type="button" class="btn-tl btn-tl-sm" data-tl-btn="' + key + '"' +
            '   title="' + esc(t("form.translitTitle")) + '" data-i18n-title="form.translitTitle">⇄</button>' +
            '</div>' +
            '<label class="correct-wrap" title="' + esc(t("form.correctAnswer")) + '" data-i18n-title="form.correctAnswer">' +
            '  <input type="radio" name="CorrectIndex" ' + (o.correct ? "checked" : "") + ' />' +
            '  <span class="correct-dot"></span>' +
            '  <span class="correct-label" data-i18n="form.correct">' + esc(t("form.correct")) + '</span>' +
            '</label>' +
            '<button type="button" class="btn-opt-del" title="' + esc(t("form.delete")) + '" data-i18n-title="form.delete">' +
            '  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">' +
            '    <polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/><path d="M10 11v6M14 11v6"/>' +
            '  </svg>' +
            '</button>';

        item.querySelector('input[type="radio"]').addEventListener("change", function () { markCorrect(item); });
        item.querySelector(".btn-opt-del").addEventListener("click", function () {
            item.remove();
            renumber();
        });

        list.appendChild(item);
        bindPair(key);
        renumber();
        return item;
    }

    /* ── Ishga tushirish ── */

    var cfg = window.QF || {};
    var initial = cfg.options && cfg.options.length ? cfg.options : [{}, {}, {}, {}];
    initial.forEach(addOption);

    document.getElementById("addOptionBtn").addEventListener("click", function () {
        addOption({}).querySelector("input").focus();
    });

    ["question", "explanation"].forEach(bindPair);

    var allBtn = document.getElementById("translitAllBtn");
    if (allBtn) allBtn.addEventListener("click", translitAllEmpty);

    var form = list.closest("form");
    form.addEventListener("submit", renumber);
})();
