/*
 * Interfeys yordamchilari (bootstrap JS talab qilinmaydi):
 *   [data-dd]              — ochiladigan menyu tugmasi (.ui-dd ichida); tashqariga bosish / Esc yopadi
 *   [data-sidebar-toggle]  — kabinet/admin sidebar'ini mobil ekranda ochish
 *   [data-nav-toggle]      — sayt navigatsiyasini mobil ekranda ochish
 *   .ca-bar > i[data-w]    — progress chiziqlari animatsiyasi
 */
(function () {
    "use strict";

    function closeAll(except) {
        document.querySelectorAll(".ui-dd.open").forEach(function (dd) {
            if (dd !== except) {
                dd.classList.remove("open");
                var b = dd.querySelector("[data-dd]");
                if (b) b.setAttribute("aria-expanded", "false");
            }
        });
    }

    document.addEventListener("click", function (e) {
        var btn = e.target.closest("[data-dd]");
        if (btn) {
            e.preventDefault();
            var dd = btn.closest(".ui-dd");
            var open = !dd.classList.contains("open");
            closeAll(dd);
            dd.classList.toggle("open", open);
            btn.setAttribute("aria-expanded", open ? "true" : "false");
            if (open) {
                var first = dd.querySelector(".ui-dd-menu .active, .ui-dd-menu [role^=menuitem]");
                if (first && e.detail === 0) first.focus(); // klaviatura bilan ochilganda
            }
            return;
        }

        // Menyu bandi tanlandi (masalan, til) — yopamiz
        if (e.target.closest(".ui-dd-menu [data-set-lang]")) { setTimeout(function () { closeAll(null); }, 0); return; }
        if (!e.target.closest(".ui-dd")) closeAll(null);

        var sb = e.target.closest("[data-sidebar-toggle]");
        if (sb) { document.body.classList.toggle("sidebar-open"); return; }
        if (e.target.closest(".ca-overlay")) { document.body.classList.remove("sidebar-open"); return; }

        var nav = e.target.closest("[data-nav-toggle]");
        if (nav) {
            var header = nav.closest(".ui-header");
            var isOpen = header.classList.toggle("nav-open");
            nav.setAttribute("aria-expanded", isOpen ? "true" : "false");
        }
    });

    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape") {
            closeAll(null);
            document.body.classList.remove("sidebar-open");
            document.querySelectorAll(".ui-header.nav-open").forEach(function (h) { h.classList.remove("nav-open"); });
            return;
        }

        // Menyu ichida ↑/↓ bilan harakatlanish
        var menu = e.target.closest && e.target.closest(".ui-dd.open .ui-dd-menu");
        if (menu && (e.key === "ArrowDown" || e.key === "ArrowUp")) {
            e.preventDefault();
            var items = Array.prototype.slice.call(menu.querySelectorAll("[role^=menuitem]"));
            var i = items.indexOf(document.activeElement);
            var next = e.key === "ArrowDown" ? (i + 1) % items.length : (i - 1 + items.length) % items.length;
            if (items[next]) items[next].focus();
        }
    });

    // Ekran kengayganda mobil menyularni yopamiz
    window.addEventListener("resize", function () {
        if (window.innerWidth > 1024) document.body.classList.remove("sidebar-open");
        if (window.innerWidth > 1399) document.querySelectorAll(".ui-header.nav-open").forEach(function (h) { h.classList.remove("nav-open"); });
    });

    function animateBars() {
        var bars = document.querySelectorAll(".ca-bar > i[data-w]");
        if (!("IntersectionObserver" in window)) {
            bars.forEach(function (b) { b.style.width = b.getAttribute("data-w") + "%"; });
            return;
        }
        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (en) {
                if (en.isIntersecting) {
                    en.target.style.width = en.target.getAttribute("data-w") + "%";
                    io.unobserve(en.target);
                }
            });
        }, { threshold: 0.2 });
        bars.forEach(function (b) { io.observe(b); });
    }

    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", animateBars);
    else animateBars();
})();
