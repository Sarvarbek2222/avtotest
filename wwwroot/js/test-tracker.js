/*
 * Test natijalarini foydalanuvchi kabinetiga yozib olish.
 *
 * Test sahifalari (Views/Test*, Bilet/TestPage, Topic/Test) O'ZGARTIRILMAGAN — bu skript ularga umumiy
 * _I18nScripts.cshtml orqali ulanadi va sahifadagi mavjud global funksiyalarni o'rab oladi:
 *   selectAnswer(q, o) — yangi urinish boshlanganini aniqlash (answers obyekti almashsa — yangi urinish),
 *   finishTest()       — "Yakunlash" (yoki barcha savollar javoblangach) natijani serverga yuborish.
 * Sahifaning `questions`, `originalQuestions`, `answers` o'zgaruvchilaridan faqat o'qiladi.
 *
 * Server to'g'ri/xatoni o'zi hisoblaydi (bazadagi variantlar bo'yicha). Bir urinish qayta yuborilsa
 * (masalan, "Yakunlash" ikki marta bosilsa) — yangilanadi, dublikat bo'lmaydi.
 */
(function () {
    "use strict";

    var cfg = window.TEST_TRACKER;
    if (!cfg || !cfg.type) return;

    function newKey() {
        if (window.crypto && crypto.randomUUID) return crypto.randomUUID();
        return Date.now().toString(36) + "-" + Math.random().toString(36).slice(2) + Math.random().toString(36).slice(2);
    }

    // Sahifa o'zgaruvchilari boshqa <script> da `let` bilan e'lon qilingan — global leksik muhitdan o'qiymiz
    function read(name) {
        try {
            switch (name) {
                case "questions": return typeof questions !== "undefined" ? questions : null;
                case "originalQuestions": return typeof originalQuestions !== "undefined" ? originalQuestions : null;
                case "answers": return typeof answers !== "undefined" ? answers : null;
            }
        } catch (e) { /* e'lon qilinmagan */ }
        return null;
    }

    var attempt = { key: newKey(), answersRef: null, startedAt: Date.now() };

    function ensureAttempt() {
        var a = read("answers");
        if (a && attempt.answersRef && a !== attempt.answersRef) {
            // "Qayta ishlash" yoki "Xatolarni ishlash" — yangi urinish
            attempt = { key: newKey(), answersRef: a, startedAt: Date.now() };
        } else if (a && !attempt.answersRef) {
            attempt.answersRef = a;
        }
    }

    function t(key, args) {
        return window.I18n ? I18n.t(key, args) : key;
    }

    function toast(ok, text) {
        var el = document.createElement("div");
        el.setAttribute("role", "status");
        el.textContent = text;
        el.style.cssText =
            "position:fixed;right:16px;bottom:16px;z-index:6000;max-width:calc(100vw - 32px);" +
            "padding:10px 16px;border-radius:10px;font:600 14px/1.4 system-ui,sans-serif;color:#fff;" +
            "box-shadow:0 8px 24px rgba(0,0,0,.35);opacity:0;transform:translateY(8px);transition:all .25s;" +
            "background:" + (ok ? "#16a34a" : "#b91c1c");
        document.body.appendChild(el);
        requestAnimationFrame(function () { el.style.opacity = "1"; el.style.transform = "none"; });
        setTimeout(function () {
            el.style.opacity = "0";
            setTimeout(function () { el.remove(); }, 300);
        }, 3200);
    }

    function save() {
        var qs = read("questions");
        var ans = read("answers");
        if (!qs || !qs.length || !ans) return;

        ensureAttempt();

        var items = qs.map(function (q) {
            var sel = ans[q.id];
            return { questionId: q.id, selectedOptionId: sel === undefined || sel === null ? null : Number(sel) };
        });
        if (!items.some(function (i) { return i.selectedOptionId !== null; })) return;

        var orig = read("originalQuestions");
        var payload = {
            attemptKey: attempt.key,
            testType: cfg.type,
            testRef: cfg.ref || null,
            // Test ichidagi "Xatolarni ishlash" — savollar asl ro'yxatdan kam bo'ladi
            isErrorReview: cfg.type !== "mistakes" && !!orig && qs.length < orig.length,
            startedAt: attempt.startedAt,
            items: items
        };

        fetch("/Cabinet/SaveResult", {
            method: "POST",
            credentials: "same-origin",
            keepalive: true,
            headers: { "Content-Type": "application/json", "Accept": "application/json" },
            body: JSON.stringify(payload)
        }).then(function (r) {
            if (!r.ok) throw new Error("HTTP " + r.status);
            return r.json();
        }).then(function () {
            toast(true, "✓ " + t("tracker.saved"));
        }).catch(function () {
            toast(false, t("tracker.failed"));
        });
    }

    function wrap(name, before, after) {
        var orig = window[name];
        if (typeof orig !== "function") return false;
        window[name] = function () {
            if (before) { try { before.apply(this, arguments); } catch (e) { /* kuzatuv xatosi testga ta'sir qilmasin */ } }
            var result = orig.apply(this, arguments);
            if (after) { try { after.apply(this, arguments); } catch (e) { /* ... */ } }
            return result;
        };
        return true;
    }

    document.addEventListener("DOMContentLoaded", function () {
        wrap("selectAnswer", ensureAttempt, null);
        wrap("finishTest", null, save);
    });
})();
