let currentLang = "uz";

window.onload = function () {
    // Cookie orqali tilni olish
    const match = document.cookie.match(/lang=(.+?)(;|$)/);
    if (match) currentLang = match[1];

    // Til tugmalari
    document.getElementById('langUZ').onclick = () => changeLanguage('uz');
    document.getElementById('langRU').onclick = () => changeLanguage('ru');
    document.getElementById('langEN').onclick = () => changeLanguage('en');

    // Boshlang‘ich holat
    changeLanguage(currentLang);
};

function changeLanguage(lang) {
    currentLang = lang;

    // Navbar va matnlarni o‘zgartirish
    fetch(`/Home/GetTexts?lang=${lang}`)
        .then(res => res.json())
        .then(data => {
            document.getElementById('nav40').innerText = data["40"];
            document.getElementById('nav60').innerText = data["60"];
            document.getElementById('nav8').innerText = data["8"];
            document.getElementById('nav100').innerText = data["100"];
            document.getElementById('nav160').innerText = data["160"];
            document.getElementById('navReal').innerText = data["Real"];

            document.getElementById('welcome').innerText = data.Welcome;
            document.getElementById('description').innerText = data.Description;
        })
        .catch(err => console.error("GetTexts xatolik:", err));

    loadQuestions(lang);

    // Tilni cookie’da saqlash
    document.cookie = "lang=" + lang + "; path=/; max-age=" + 60 * 60 * 24 * 30;
}

function loadQuestions(lang) {
    fetch(`/Home/GetQuestions?lang=${lang}`)
        .then(res => res.json())
        .then(data => {
            const container = document.getElementById('test-container');
            container.innerHTML = "";

            if (!data || !Array.isArray(data)) {
                console.error("Savollar topilmadi yoki data noto‘g‘ri:", data);
                container.innerHTML = "<p>Savollar topilmadi...</p>";
                return;
            }

            data.forEach((q, idx) => {
                const div = document.createElement('div');
                div.classList.add('mb-3', 'p-3', 'border', 'shadow-sm');

                let optionsHTML = "";
                if (q.options && Array.isArray(q.options)) {
                    q.options.forEach(o => {
                        optionsHTML += `<div class="form-check">
                            <input class="form-check-input" type="radio" name="q${q.id}" value="${o.id}" data-correct="${o.isCorrect}">
                            <label class="form-check-label">${o.text}</label>
                        </div>`;
                    });
                }

                div.innerHTML = `<p><strong>${idx + 1}. ${q.question}</strong></p>${optionsHTML}`;
                container.appendChild(div);
            });
        })
        .catch(err => console.error("GetQuestions xatolik:", err));
}

// Testni yakunlash va natijani hisoblash
function submitTest() {
    const radios = document.querySelectorAll('#test-container input[type="radio"]:checked');
    if (radios.length === 0) {
        alert("Iltimos, savollarga javob bering!");
        return;
    }

    let correct = 0;
    radios.forEach(r => {
        if (r.dataset.correct === "True") correct++;
    });

    alert(`To‘g‘ri javoblar: ${correct} / ${radios.length}`);
}