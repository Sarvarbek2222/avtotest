#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import re, sys

def uz_to_cyrillic(text):
    result = []
    i = 0
    while i < len(text):
        c2 = text[i:i+2]
        c1 = text[i]
        if c2 in ("o'", "O'"): result.append("ў" if c1=='o' else "Ў"); i+=2; continue
        if c2 in ("g'", "G'"): result.append("ғ" if c1=='g' else "Ғ"); i+=2; continue
        if c2.lower() == "sh": result.append("ш" if c1=='s' else "Ш"); i+=2; continue
        if c2.lower() == "ch": result.append("ч" if c1=='c' else "Ч"); i+=2; continue
        if c2.lower() == "ng": result.append("нг" if c1=='n' else "Нг"); i+=2; continue
        if c2.lower() == "ts": result.append("тс" if c1=='t' else "Тс"); i+=2; continue
        m = {'a':'а','A':'А','b':'б','B':'Б','d':'д','D':'Д','e':'е','E':'Е',
             'f':'ф','F':'Ф','g':'г','G':'Г','h':'ҳ','H':'Ҳ','i':'и','I':'И',
             'j':'ж','J':'Ж','k':'к','K':'К','l':'л','L':'Л','m':'м','M':'М',
             'n':'н','N':'Н','o':'о','O':'О','p':'п','P':'П','q':'қ','Q':'Қ',
             'r':'р','R':'Р','s':'с','S':'С','t':'т','T':'Т','u':'у','U':'У',
             'v':'в','V':'В','x':'х','X':'Х','y':'й','Y':'Й','z':'з','Z':'З'}
        result.append(m.get(c1, c1)); i+=1
    return ''.join(result)

RU = {
"Birinchi tibbiy yordam qoidalari: Baxtsiz hodisada birinchi tibbiy yordam: xavfsiz joyga ko\\'chirish (kerak bo\\'lsa), nafas va qon aylanishini tekshirish, qon to\\'xtatish, tez yordam chaqirish (103 yoki 112).":
"Правила первой медицинской помощи: При ДТП: переместить пострадавшего в безопасное место (при необходимости), проверить дыхание и кровообращение, остановить кровотечение, вызвать скорую помощь (103 или 112).",

"Birinchi tibbiy yordam qoidalari: Qon to\\'xtatish uchun qo\\'l yoki oyoqqa jigarband (tomir bog\\'lash) qo\\'yiladi, 2 soatdan ortiq tutmaslik kerak. Yumshoq to\\'qimaga bosim bog\\'lami qo\\'llaniladi.":
"Правила первой медицинской помощи: Для остановки кровотечения на руку или ногу накладывается жгут, держать не более 2 часов. На мягкие ткани накладывается давящая повязка.",

"Birinchi tibbiy yordam qoidalari: Shok holati — organizmning kuchli ta\\'sirga javobi. Shok bo\\'lganda: jabrlanuvchini yotqizish, oyoqlarini ko\\'tarish, isitish, tez yordam chaqirish.":
"Правила первой медицинской помощи: Шок — реакция организма на сильное воздействие. При шоке: уложить пострадавшего, поднять ноги, согреть, вызвать скорую помощь.",

"Birinchi tibbiy yordam qoidalari: Sun\\'iy nafas berish — og\\'iz-og\\'iz usuli. Ko\\'krak qafasini bosish (CPR) 30:2 nisbatda — 30 ta bosish, 2 ta nafas. Bu amallar tibbiy yordam kelguncha bajariladi.":
"Правила первой медицинской помощи: Искусственное дыхание — метод «рот в рот». Непрямой массаж сердца (СЛР) в соотношении 30:2 — 30 нажатий, 2 вдоха. Выполнять до прибытия медицинской помощи.",

"Birinchi tibbiy yordam qoidalari: Suyak siniqlarida: immobilizatsiya (harakatlanishni to\\'xtatish), jarohat joyiga qattiq narsa ilib, bandaj bog\\'lash. Umurtqa shikastida — qimirlatmaslik shart.":
"Правила первой медицинской помощи: При переломах: иммобилизация (обездвиживание), наложить шину на место повреждения, перевязать. При повреждении позвоночника — не двигать пострадавшего.",

"Birinchi tibbiy yordam qoidalari: Zaharlanishda: zaharli gaz bo\\'lsa — toza havoga olib chiqish, ong yo\\'q bo\\'lsa — yon tomoniga yotqizish, sun\\'iy nafas berish.":
"Правила первой медицинской помощи: При отравлении: если отравление газом — вынести на свежий воздух, при потере сознания — положить на бок, провести искусственное дыхание.",

"YHQ 10-bob 60-band: Aholisi bo\\'lgan joylarda — 60 km/soat, shahar tashqarisida — 90 km/soat, avtomagistrallarda — 110 km/soat, boshqa transport vositalarini shatakka olayotganda — 50 km/soat chegarasi.":
"ПДД Гл.10 П.60: В населённых пунктах — 60 км/ч, за городом — 90 км/ч, на автомагистралях — 110 км/ч, при буксировке других ТС — 50 км/ч.",

"YHQ 10-bob 60-band: Maktab va maktabgacha ta\\'lim muassasalari oldida harakatlanish tezligi 30 km/soatdan oshmasligi kerak (1.23 belgisi o\\'rnatilgan joyda).":
"ПДД Гл.10 П.60: Вблизи школ и дошкольных учреждений скорость не должна превышать 30 км/ч (в зоне знака 1.23).",

"YHQ 10-bob 60-band: Shahar tashqarisidagi yo\\'llarda — 90 km/soat, avtomagistrallarda — 110 km/soat, aholisi bo\\'lgan joylarda — 70 km/soat (toifa belgisi bo\\'lsa), odatda 60 km/soat chegarasi qo\\'llaniladi.":
"ПДД Гл.10 П.60: На дорогах за городом — 90 км/ч, на автомагистралях — 110 км/ч, в населённых пунктах — 70 км/ч (при знаке категории), как правило, ограничение 60 км/ч.",

"YHQ 10-bob 60-band: \\'U\\' taniqlik belgisi o\\'rnatilgan transport vositasi uchun tezlik 70 km/soatdan oshmasligi kerak.":
"ПДД Гл.10 П.60: Для транспортного средства с опознавательным знаком «У» скорость не должна превышать 70 км/ч.",

"YHQ 10-bob 63-band: Sirpanchiq yo\\'lda tezlikni kamaytirish, to\\'satdan tormozlamaslik, keskin burilmaslik shart. Tormozlash bosqichma-bosqich amalga oshiriladi.":
"ПДД Гл.10 П.63: На скользкой дороге необходимо снизить скорость, не тормозить резко, не делать резких поворотов. Торможение осуществляется постепенно.",

"YHQ 10-bob 64-band: Tormozlanish yo\\'li tezlikka bog\\'liq. Reaktsiya vaqti, tormoz qo\\'yish vaqti va to\\'xtash yo\\'lidan iborat. Tezlik 2 marta oshsa — tormozlanish yo\\'li 4 marta uzayadi.":
"ПДД Гл.10 П.64: Тормозной путь зависит от скорости. Состоит из времени реакции, срабатывания тормозов и пути торможения. При увеличении скорости в 2 раза — тормозной путь увеличивается в 4 раза.",

"YHQ 10-bob 64-band: Tormozlanish yo\\'li transport vositasining tezligiga bog\\'liq. Tezlik 2 marta oshsa, tormozlanish yo\\'li 4 marta uzayadi (tezlik kvadratiga proporsional).":
"ПДД Гл.10 П.64: Тормозной путь зависит от скорости транспортного средства. При увеличении скорости в 2 раза тормозной путь увеличивается в 4 раза (пропорционально квадрату скорости).",

"YHQ 10-bob: Harakatlanish tezligi belgilangan me\\'yordan oshmasligi kerak. Yo\\'l sharoiti, ob-havo va ko\\'rish masofasiga qarab tezlikni kamaytirish shart.":
"ПДД Гл.10: Скорость движения не должна превышать установленного норматива. В зависимости от дорожных условий, погоды и видимости необходимо снижать скорость.",

"YHQ 10-bob: Tezlikni km/soatdan m/sekundga aylantirish formulasi: v(m/s) = v(km/soat) / 3.6. Masalan, 72 km/soat = 20 m/sekund.":
"ПДД Гл.10: Формула перевода скорости из км/ч в м/с: v(м/с) = v(км/ч) / 3,6. Например, 72 км/ч = 20 м/с.",

"YHQ 11-bob 71-75-bandlar: Quvib o\\'tish — transport vositasini yo\\'lning qarama-qarshi yo\\'nalish tomonga chiqib oldinga o\\'tish. Burilish chiroqlarini yoqib, belgilangan tartibda amalga oshiriladi.":
"ПДД Гл.11 П.71-75: Обгон — опережение ТС с выездом на полосу встречного движения. Выполняется с включением указателей поворота в установленном порядке.",

"YHQ 11-bob 72-band: Quvib o\\'tishdan oldin haydovchi: oldindagi transport vositasi bilan yetarli masofa borligini, teskari yo\\'nalishdan transport vositasi kelmasligini, quvib o\\'tishdan keyin xavfsiz joy borligini tekshirishi shart.":
"ПДД Гл.11 П.72: Перед обгоном водитель обязан убедиться: в наличии достаточного расстояния до ТС впереди, в отсутствии встречного транспорта, в наличии безопасного места после обгона.",

"YHQ 11-bob 72-band: Quvib o\\'tish faqat chap tomondan ruxsat etiladi. O\\'ng tomondan quvib o\\'tish faqat quvib o\\'tilayotgan transport vositasi chap burilish ishora berganda yoki tramvay yo\\'lidan o\\'tishda ruxsat etiladi.":
"ПДД Гл.11 П.72: Обгон разрешён только слева. Обгон справа разрешён, только если обгоняемое ТС подаёт сигнал левого поворота или при объезде трамвайного пути.",

"YHQ 11-bob 74-band: Quvib o\\'tish taqiqlanadigan holatlar: ko\\'rish maydonini cheklaydigan joylarda, chorrahada, piyodalar o\\'tish joyida, temir yo\\'l kesishmasida, ko\\'prikda va tunnel ichida quvib o\\'tish taqiqlanadi.":
"ПДД Гл.11 П.74: Обгон запрещён: в местах с ограниченной видимостью, на перекрёстках, пешеходных переходах, железнодорожных переездах, мостах и в тоннелях.",

"YHQ 12-bob 75-band: Piyodalar o\\'tish joyida (zebra, 5.19 belgisi yoki svetofor) haydovchilar piyodalarga yo\\'l berishlari shart. Piyodalar ham svetofor ishoralariga rioya qilishlari kerak.":
"ПДД Гл.12 П.75: На пешеходном переходе (зебра, знак 5.19 или светофор) водители обязаны уступать дорогу пешеходам. Пешеходы также должны соблюдать сигналы светофора.",

"YHQ 12-bob 76-band: Yo\\'nalishli transport vositasi bekatda to\\'xtab, yo\\'lovchi tushirishi-chiqarishida boshqa transport vositalari yo\\'l berishlari shart. Bekat zonasida quvib o\\'tish taqiqlanadi.":
"ПДД Гл.12 П.76: При остановке маршрутного ТС на остановке для посадки-высадки пассажиров другие ТС обязаны уступить дорогу. В зоне остановки обгон запрещён.",

"YHQ 13-bob 85-92-bandlar: To\\'xtash — transport vositasini 5 daqiqagacha to\\'xtatish. To\\'xtab turish — 5 daqiqadan ortiq to\\'xtatish. Yo\\'lning o\\'ng tomonida, belgiga qarab ruxsat etilgan joyda to\\'xtatish lozim.":
"ПДД Гл.13 П.85-92: Остановка — прекращение движения ТС до 5 минут. Стоянка — более 5 минут. Останавливаться следует с правой стороны дороги, в разрешённом знаком месте.",

"YHQ 13-bob 85-band: Qatnov qismining kengaytirilmagan joylarida transport vositalarini faqat qatnov qismining o\\'ng chekkasiga parallel qo\\'yish ruxsat etiladi.":
"ПДД Гл.13 П.85: В местах без расширения проезжей части ТС разрешается ставить только параллельно правому краю проезжей части.",

"YHQ 13-bob 90-band: Belgilangan piyodalar o\\'tish joyida va undan 5 m yaqinida to\\'xtash taqiqlanadi.":
"ПДД Гл.13 П.90: Остановка запрещена на пешеходном переходе и ближе 5 м от него.",

"YHQ 13-bob 90-band: Chorrahada (yo\\'llar tutashadigan joydan 5 m ichida) to\\'xtash taqiqlanadi. Bundan tashqari, shaharda asosiy yo\\'llarda to\\'xtab turish taqiqlanadi.":
"ПДД Гл.13 П.90: На перекрёстке (в пределах 5 м от пересечения проезжих частей) остановка запрещена. Кроме того, на главных дорогах в городе стоянка запрещена.",

"YHQ 13-bob 90-band: Transport vositasini to\\'xtashni taqiqlaydigan joylar belgilangan. To\\'xtash — haydovchi transport vositasini 5 daqiqadan ko\\'p bo\\'lmagan muddatga to\\'xtatishi.":
"ПДД Гл.13 П.90: Определены места, где остановка ТС запрещена. Остановка — прекращение движения ТС на срок не более 5 минут.",

"YHQ 13-bob 90-band: Yo\\'nalishli transport vositalarining belgilangan bekati yaqinida (15 m dan yaqin) to\\'xtash taqiqlanadi, bundan tashqarilarda ruxsat etiladi.":
"ПДД Гл.13 П.90: Остановка запрещена вблизи (ближе 15 м) обозначенной остановки маршрутных ТС, в остальных местах — разрешена.",

"YHQ 13-bob 91-band: Transport vositasini to\\'xtab turishni taqiqlaydigan joylar belgilangan. To\\'xtab turish — haydovchi transport vositasini yo\\'lovchi tushirish-chiqarish yoki yuk ortish-tushirishdan tashqari, 5 daqiqadan ko\\'p vaqt davomida qoldirishi.":
"ПДД Гл.13 П.91: Определены места, где стоянка ТС запрещена. Стоянка — оставление ТС более чем на 5 минут, кроме посадки-высадки пассажиров или погрузки-разгрузки.",

"YHQ 14-bob 79-86-bandlar: Chorrahada harakatlanish tartibi: tartiblashtiruvchi, svetofor, belgilar va o\\'ng tomondan xalaqit qoidasi asosida aniqlanadi.":
"ПДД Гл.14 П.79-86: Порядок движения на перекрёстке определяется: регулировщиком, светофором, знаками и правилом «помехи справа».",

"YHQ 14-bob 79-band: Asosiy yo\\'ldan harakatlanayotgan transport vositasi ikkinchi darajali yo\\'ldagi transport vositasidan ustunlikka ega. 2.1, 2.3.1-2.3.7 belgilari asosiy yo\\'lni belgilaydi.":
"ПДД Гл.14 П.79: ТС, движущееся по главной дороге, имеет преимущество перед ТС на второстепенной дороге. Главную дорогу обозначают знаки 2.1, 2.3.1–2.3.7.",

"YHQ 14-bob 79-band: Tartibga solinadigan chorrahada haydovchilar svetofor yoki tartiblashtiruvchi xodimlari ishoralariga rioya qilishlari shart. Svetofor ko\\'k (yashil) yoritsa — harakatlanish ruxsat etiladi.":
"ПДД Гл.14 П.79: На регулируемом перекрёстке водители обязаны соблюдать сигналы светофора или регулировщика. Зелёный сигнал светофора — движение разрешено.",

"YHQ 14-bob 80-band: Teng ahamiyatli tartibga solinmagan chorrahada \\'o\\'ng tomondan xalaqit\\' qoidasi amal qiladi — o\\'ngdan kelayotgan transport vositasiga yo\\'l berish shart.":
"ПДД Гл.14 П.80: На равнозначном нерегулируемом перекрёстке действует правило «помехи справа» — необходимо уступить дорогу ТС, приближающемуся справа.",

"YHQ 14-bob 81-band: Aylanma harakatli chorrahada (4.3 belgisi o\\'rnatilgan) aylanmada harakatlanayotgan transport vositasi ustunlikka ega. Boshqa transport vositalari yo\\'l berishlari shart.":
"ПДД Гл.14 П.81: На перекрёстке с круговым движением (знак 4.3) преимущество имеет ТС, движущееся по кольцу. Остальные ТС обязаны уступить дорогу.",

"YHQ 15-bob 92-96-bandlar: Temir yo\\'l kesishmasiga yaqinlashganda tezlikni kamaytirish va ehtiyotkor bo\\'lish shart. Shlagbaum, svetofor yoki qo\\'ng\\'iroqqa rioya qilish majburiy.":
"ПДД Гл.15 П.92-96: При приближении к железнодорожному переезду необходимо снизить скорость и соблюдать осторожность. Обязательно соблюдение шлагбаума, светофора или звуковых сигналов.",

"YHQ 15-bob 92-band: Shlagbaum yopiq bo\\'lsa yoki yopilayotgan bo\\'lsa, qizil chiroq yonsa yoki qo\\'ng\\'iroq yangrasa — kesishma oldida to\\'xtash va yo\\'l berib turish majburiy.":
"ПДД Гл.15 П.92: Если шлагбаум закрыт или закрывается, горит красный сигнал или звучит звонок — необходимо остановиться перед переездом и ждать.",

"YHQ 15-bob 92-band: Temir yo\\'l kesishmasidan o\\'tish uchun: svetofor ko\\'k (yashil) yonishi, shlagbaum ochiq bo\\'lishi va temir yo\\'lda poyezd ko\\'rinmasligi kerak. Barcha shart bajarilganda o\\'tish mumkin.":
"ПДД Гл.15 П.92: Для проезда через железнодорожный переезд: светофор горит зелёным, шлагбаум открыт, поезд не виден. Проехать можно только при выполнении всех условий.",

"YHQ 15-bob 94-band: Temir yo\\'l kesishmasida transport vositasi to\\'xtab qolsa, yo\\'lovchilarni tushirish va transport vositasini darhol yilg\\'ib olish kerak. To\\'xtash chizig\\'ida to\\'xtash shart.":
"ПДД Гл.15 П.94: Если ТС заглохло на железнодорожном переезде, необходимо высадить пассажиров и немедленно убрать ТС. Остановка у стоп-линии обязательна.",

"YHQ 15-bob 95-band: Temir yo\\'l kesishmasida to\\'xtab turish taqiqlanadi. Poyezd o\\'tib ketgandan keyin ham agar ikkinchi yo\\'lda poyezd ko\\'rinsa, to\\'xtashni davom ettirish kerak.":
"ПДД Гл.15 П.95: Стоянка на железнодорожном переезде запрещена. Даже после прохождения поезда, если на другом пути виден ещё один поезд, необходимо продолжать ждать.",

"YHQ 17-bob 105-108-bandlar: Qiyaliqlarda harakatlanishda: pastga tushishda dvigatel tormozidan foydalanish, yuqoriga ko\\'tarilishda momentum saqlash. Tor qiyalikda yuqoriga harakatlanayotgan ustuvor.":
"ПДД Гл.17 П.105-108: При движении на подъёмах и спусках: при спуске использовать торможение двигателем, при подъёме сохранять инерцию. На узком подъёме преимущество у движущегося вверх.",

"YHQ 17-bob 107-band: Tik nishabliklarda tormozlanishda ajratilgan uzatmadan emas, dvigatel tormozidan foydalanish kerak. Uzatmani ajratib tormozlash — tormoz tizimini qizitib, ishdan chiqaradi.":
"ПДД Гл.17 П.107: При торможении на крутых спусках следует использовать торможение двигателем, а не выключенную передачу. Торможение с выключенной передачей перегревает тормозную систему и выводит её из строя.",

"YHQ 18-bob 110-114-bandlar: Avtomagistral — haydovchilar uchun maxsus qurilgan, yuqori tezlikdagi yo\\'l. Bu yo\\'llarda maksimal tezlik 110 km/soat, minimal 40 km/soat.":
"ПДД Гл.18 П.110-114: Автомагистраль — скоростная дорога, специально построенная для водителей. Максимальная скорость — 110 км/ч, минимальная — 40 км/ч.",

"YHQ 18-bob 112-band: Avtomagistrallarda taqiqlanadi: piyoda yurish, velosiped, moped va qishloq xo\\'jaligi texnikasining harakati, qaytib olish, orqaga yurish, harakatlanishning to\\'liq to\\'xtab qolishi.":
"ПДД Гл.18 П.112: На автомагистралях запрещается: движение пешеходов, велосипедов, мопедов и сельхозтехники, разворот, движение задним ходом, полная остановка движения.",

"YHQ 18-bob 113-band: Avtomagistrallarda to\\'xtash faqat belgilangan to\\'xtash joylarida va favqulodda holatlarda (o\\'ng tomonda) ruxsat etiladi. Favqulodda to\\'xtashda avariya chirog\\'ini yoqib, to\\'xtash belgisini qo\\'yish shart.":
"ПДД Гл.18 П.113: На автомагистралях остановка разрешена только в обозначенных местах отдыха и в аварийных ситуациях (на правой обочине). При аварийной остановке включить аварийную сигнализацию и выставить знак остановки.",

"YHQ 19-bob 115-117-bandlar: Yuk tashishda: yuk mahkam o\\'rnatilishi, gabaritdan chiqib tursa belgilanishi, to\\'kilmasligi va boshqa haydovchilarni xavf ostiga qo\\'ymasligi kerak.":
"ПДД Гл.19 П.115-117: При перевозке груза: груз должен быть надёжно закреплён, если выступает за габариты — обозначен, не должен осыпаться и создавать опасность для других водителей.",

"YHQ 19-bob 115-band: Yuk transport vositasining gabaritidan orqada 2 m dan ortiq chiqsa — ogohlantiruvchi (qizil bayroqcha, tun va tuman bo\\'lsa chiroq) bilan belgilanishi shart.":
"ПДД Гл.19 П.115: Если груз выступает сзади за габариты ТС более чем на 2 м — должен быть обозначен предупреждающим сигналом (красный флажок, а ночью и в туман — фонарь).",

"YHQ 19-bob 115-band: Yuk transport vositasining texnik hujjatida ko\\'rsatilgan yuklanish me\\'yoridan oshmasligi kerak. Me\\'yordan oshib ketsa — maxsus ruxsatnoma talab etiladi.":
"ПДД Гл.19 П.115: Нагрузка ТС не должна превышать норму, указанную в технических документах. При превышении нормы — требуется специальное разрешение.",

"YHQ 19-bob 116-118-bandlar: Turar joy dahalarida: tezlik 20 km/soat, piyodalar ustuvorlikka ega, bolalar o\\'ynashi mumkin. Haydovchilar ehtiyotkorlik bilan harakatlanishi shart.":
"ПДД Гл.19 П.116-118: В жилых зонах: скорость 20 км/ч, пешеходы имеют преимущество, дети могут играть. Водители обязаны двигаться с особой осторожностью.",

"YHQ 19-bob 116-band: Turar joy dahalarida taqiqlanadi: transport vositasini dahliz ichida to\\'xtab qoldirish (yo\\'lovchi tushirish bundan mustasno), dvigatel isitish (10 min dan ortiq).":
"ПДД Гл.19 П.116: В жилых зонах запрещается: оставлять ТС в зоне (кроме высадки пассажиров), прогревать двигатель более 10 минут.",

"YHQ 1-bob 1-5-bandlar: Yo\\'l harakati qoidalari barcha haydovchilar, piyodalar, yo\\'lovchilar va boshqa ishtirokchilar uchun majburiy. Qoidalar buzilishi jazo choralarini keltirib chiqaradi.":
"ПДД Гл.1 П.1-5: Правила дорожного движения обязательны для всех водителей, пешеходов, пассажиров и других участников движения. Нарушение правил влечёт применение мер наказания.",

"YHQ 1-bob 1-band: Barcha yo\\'l harakati ishtirokchilari yo\\'l harakati xavfsizligini ta\\'minlash majburiyatini bajarishlari shart. Yo\\'l qoidalari barcha haydovchilar, piyodalar va yo\\'lovchilar uchun majburiy.":
"ПДД Гл.1 П.1: Все участники дорожного движения обязаны обеспечивать его безопасность. Правила дорожного движения обязательны для всех водителей, пешеходов и пассажиров.",

"YHQ 20-bob 117-123-bandlar: Shatakka olish: egiluvchan yoki qattiq bog\\'lam bilan amalga oshiriladi. Shatakka olinayotgan transport vositasida haydovchi bo\\'lishi shart (qattiq birikmadan tashqari). Tezlik 50 km/soat.":
"ПДД Гл.20 П.117-123: Буксировка осуществляется гибкой или жёсткой сцепкой. В буксируемом ТС должен находиться водитель (кроме жёсткой сцепки). Скорость — 50 км/ч.",

"YHQ 20-bob 119-band: Egiluvchan bog\\'lam (tros) bilan shatakka olishda bog\\'lam qizil-oq rangli bayroqchalar yoki yorug\\'lik qaytaruvchi elementlar bilan belgilanishi shart. Uzunligi 4-6 metr.":
"ПДД Гл.20 П.119: При буксировке гибкой сцепкой (тросом) она должна быть обозначена красно-белыми флажками или световозвращающими элементами. Длина — 4–6 метров.",

"YHQ 20-bob 122-band: Shatakka olish taqiqlanadi: tormoz tizimi ishlamayotgan bo\\'lsa (qattiq birikmadan tashqari), rulevoy boshqaruv ishlamayotganda, ikki va undan ortiq transport vositasi shatakda bo\\'lsa.":
"ПДД Гл.20 П.122: Буксировка запрещена: если тормозная система неисправна (кроме жёсткой сцепки), если рулевое управление не работает, если в сцепке два и более ТС.",

"YHQ 20-bob: Avtopoezd (tirkamali transport vositasi) harakatlanishida burilish radiusi katta, yo\\'lakni egallash katta bo\\'ladi. Haydovchi tirkamaning chayqalishi va burilish markaziga siljishini hisobga olishi shart.":
"ПДД Гл.20: При движении автопоезда (ТС с прицепом) радиус поворота увеличивается, занимаемая полоса шире. Водитель должен учитывать раскачку прицепа и его смещение к центру поворота.",

"YHQ 21-bob 123-126-bandlar: Yo\\'lovchilarni tashishda: yo\\'lovchilar soni avtomobil hujjatida ko\\'rsatilganidan oshmasligi, xavfsizlik kamarlarini taqib olishlari shart.":
"ПДД Гл.21 П.123-126: При перевозке пассажиров: количество пассажиров не должно превышать указанного в документах, они обязаны пристегнуться ремнями безопасности.",

"YHQ 21-bob 124-band: 12 yoshgacha bo\\'lgan bolalarni yengil avtomobilning old o\\'rindig\\'ida faqat maxsus bola o\\'rindig\\'iga o\\'tirgan holda olib yurish ruxsat etiladi.":
"ПДД Гл.21 П.124: Детей до 12 лет разрешается перевозить на переднем сиденье легкового автомобиля только в специальном детском кресле.",

"YHQ 21-bob 124-band: Yuk avtomobilida yo\\'lovchi tashish: kabinada o\\'tirish, kuzovda o\\'tirgan holda faqat maxsus jihozlangan bo\\'lsa ruxsat etiladi. 16 yoshdan kichik bolalarni kuzovda tashish taqiqlanadi.":
"ПДД Гл.21 П.124: Перевозка пассажиров в грузовом автомобиле: в кабине — разрешено, в кузове сидя — только при специальном оборудовании. Перевозка детей до 16 лет в кузове запрещена.",

"YHQ 22-bob 127-band: Haydovlik o\\'rgatish faqat maxsus jihozlangan transport vositasida, malakali o\\'qituvchi nazoratida amalga oshiriladi. O\\'quvchini o\\'rgatishda o\\'qituvchi ham haydovchi majburiyatlarini bajaradi.":
"ПДД Гл.22 П.127: Обучение вождению осуществляется только на специально оборудованном ТС под контролем квалифицированного инструктора. При обучении инструктор также несёт обязанности водителя.",

"YHQ 24-bob 130-136-bandlar: Velosiped va moped haydovchilari uchun maxsus talablar: helmet taqish, velosiped yo\\'lagida harakatlanish, yorug\\'lik vositasiga ega bo\\'lish (kecha) majburiy.":
"ПДД Гл.24 П.130-136: Специальные требования для велосипедистов и мопедистов: обязательно надевать шлем, двигаться по велосипедной дорожке, иметь световые приборы (ночью).",

"YHQ 24-bob 131-band: Velosiped haydovchilari velosiped yo\\'lagida harakatlanishi shart. Yo\\'lak bo\\'lmasa — yo\\'l chetida, yo\\'ldoshi bilan ketayotganda — bir qatorda harakatlanishga ruxsat.":
"ПДД Гл.24 П.131: Велосипедисты обязаны двигаться по велосипедной дорожке. При её отсутствии — по краю дороги; при движении с попутчиком — допускается движение в один ряд.",

"YHQ 24-bob 133-band: Velosiped va moped haydovchilari uchun taqiqlangan: harakatlanish paytida ikki qo\\'lni qo\\'yib yuborish, 8 yoshdan kichik bolalar uchun ruxsat etilmagan joyda harakatlanish.":
"ПДД Гл.24 П.133: Велосипедистам и мопедистам запрещено: отпускать обе руки во время движения, перевозить детей до 8 лет в неустановленных местах.",

"YHQ 2-bob 14-band: Transport vositasi egalari va mansabdor shaxslar transport vositalarining texnik holati uchun javobgar. Nosoz transport vositasini yo\\'lga chiqarmaslik, ruxsatsiz o\\'zgartirishlarni amalga oshirmaslik shart.":
"ПДД Гл.2 П.14: Владельцы ТС и должностные лица несут ответственность за техническое состояние ТС. Запрещается выпускать на дорогу неисправное ТС и вносить несанкционированные изменения.",

"YHQ 3-bob 22-26-bandlar: Haydovchi transport vositasini boshqarishda: barcha hujjatlarni olib yurish, texnik ko\\'rikdan o\\'tgan transport vositasida harakatlanish, yo\\'l qoidalariga rioya qilish majburiy.":
"ПДД Гл.3 П.22-26: При управлении ТС водитель обязан: иметь при себе все документы, эксплуатировать технически исправное ТС, соблюдать правила дорожного движения.",

"YHQ 3-bob 22-band: Haydovchi transport vositasini boshqarishga to\\'liq qobiliyatli bo\\'lishi, yo\\'l sharoitiga mos tezlikda harakatlanishi va xavfsizlik qoidalariga rioya qilishi shart.":
"ПДД Гл.3 П.22: Водитель обязан быть полностью способным к управлению ТС, двигаться со скоростью, соответствующей дорожным условиям, и соблюдать правила безопасности.",

"YHQ 3-bob 22-band: Haydovchi yurish paytida majburiy: haydovchilik guvohnomasi, transport vositasining ro\\'yxatga olish guvohnomasi va sug\\'urta polisi ko\\'rsatmasi bo\\'lishi shart.":
"ПДД Гл.3 П.22: Водитель обязан при движении иметь: водительское удостоверение, свидетельство о регистрации ТС и страховой полис.",

"YHQ 3-bob 22-band: Uyquga chaqiruvchi, giyohvandlik yoki psixotrop moddalar ta\\'sirida haydovlik qilish taqiqlanadi. Bunday dorilarni iste\\'mol qilgandan so\\'ng belgilangan muddatgacha haydovlik taqiqlanadi.":
"ПДД Гл.3 П.22: Запрещается управление ТС под воздействием снотворных, наркотических или психотропных веществ. После приёма таких препаратов вождение запрещено на установленный срок.",

"YHQ 3-bob 28-band: Transport vositasining shinalar holatini nazorat qilish haydovchining majburiyati. Eskirgan, shikastlangan yoki minimal chuqurlikdan past protektor bo\\'lsa — harakatlanish taqiqlanadi.":
"ПДД Гл.3 П.28: Контроль за состоянием шин является обязанностью водителя. При износе, повреждении или протекторе ниже минимальной глубины — движение запрещено.",

"YHQ 3-bob va Ilova 8: Transport vositasidan foydalanishni taqiqlovchi texnik nosozliklar: tormoz, rulevoy, chiroqlar, shina va boshqa tizimlar nosozliklari aniqlansa — harakatlanish taqiqlanadi.":
"ПДД Гл.3 и Приложение 8: Технические неисправности, запрещающие эксплуатацию ТС: при неисправности тормозов, рулевого управления, осветительных приборов, шин и других систем — движение запрещено.",

"YHQ 4-bob 35-38-bandlar: Piyodalar trotuarda yurishi, qatnov qismini belgilangan joyda kesib o\\'tishi, transport vositalariga yo\\'l berishi va yo\\'l belgilariga rioya qilishi shart.":
"ПДД Гл.4 П.35-38: Пешеходы должны ходить по тротуару, пересекать проезжую часть в обозначенных местах, уступать дорогу ТС и соблюдать дорожные знаки.",

"YHQ 4-bob 35-band: Piyodalar trotuar va piyodalar yo\\'laklarida yurishi shart. Ular bo\\'lmasa — yo\\'l chetida (shaharda — qatnov qismining chap tomonida, qarama-qarshi yo\\'nalishda) yurish mumkin.":
"ПДД Гл.4 П.35: Пешеходы обязаны ходить по тротуарам и пешеходным дорожкам. При их отсутствии — по краю дороги (в городе — по левой стороне проезжей части, навстречу движению).",

"YHQ 4-bob 36-band: Piyodalar qatnov qismini faqat piyodalar o\\'tish joyida yoki chorrahada kesib o\\'tishlari kerak. O\\'tish yo\\'q bo\\'lganda to\\'g\\'ri burchak ostida, ko\\'rish yaxshi bo\\'lganda o\\'tishga ruxsat.":
"ПДД Гл.4 П.36: Пешеходы должны пересекать проезжую часть только на пешеходных переходах или перекрёстках. При отсутствии перехода — под прямым углом при хорошей видимости.",

"YHQ 5-bob 43-46-bandlar: Tashqi yoritish asboblarini qorong\\'i, tuman va yog\\'ingarchiliqda yoqish majburiy. Yaqin nuri shahar ichida, uzoq nuri shahar tashqarisida ishlatiladi.":
"ПДД Гл.5 П.43-46: Включение внешних световых приборов обязательно в тёмное время, в туман и при осадках. Ближний свет — в городе, дальний свет — за городом.",

"YHQ 5-bob 43-band: Qorong\\'i vaqtda va ko\\'rish chegaralanganda (300 m dan kam) chiroqlarni yoqish majburiy. Yaqin nuri — 200 m gacha, uzoq nuri — 200 m dan uzoq tozalangan yo\\'lda.":
"ПДД Гл.5 П.43: В тёмное время и при ограниченной видимости (менее 300 м) включение фар обязательно. Ближний свет — до 200 м, дальний свет — более 200 м на чистой дороге.",

"YHQ 5-bob 43-band: Tuman (ko\\'rish masofasi 100 m dan kam) bo\\'lganda old tuman chiroqlarini yoki yaqin nurli faralarni yoqish majburiy. Faqat tuman chiroqlarini yoki yaqin nuri bilan birga ishlatish mumkin.":
"ПДД Гл.5 П.43: В условиях тумана (видимость менее 100 м) обязательно включение передних противотуманных фар или фар ближнего света. Противотуманные фары используются только совместно с ближним светом.",

"YHQ 5-bob 43-band: Yon (pozitsion) chiroqlari: tun va ko\\'rish yomong\\'i bo\\'lganda yaqin yoki uzoq nuri bilan birga ishlatiladi. Mustaqil holda faqat to\\'xtatilgan transport vositasida ishlatiladi.":
"ПДД Гл.5 П.43: Боковые (габаритные) фонари используются совместно с ближним или дальним светом ночью и при плохой видимости. Самостоятельно используются только на остановившемся ТС.",

"YHQ 5-bob 44-band: Aholi punktida — faqat yaqin nuri. Shahardan tashqarida — uzoq nurdan yaqin nurga: qarshidan transport 150 m dan yaqin kelayotganda o\\'tkazish kerak.":
"ПДД Гл.5 П.44: В населённом пункте — только ближний свет. За городом — переключать с дальнего на ближний, когда встречный транспорт приближается на расстояние менее 150 м.",

"YHQ 5-bob 45-46-bandlar: Ogohlantiruv ishoralari (ovoz va yorug\\'lik) xavf haqida boshqa ishtirokchilarni ogohlantirishga xizmat qiladi. Avariya chirog\\'i — barcha burilish chiroqlari birgalikda miltillaydi.":
"ПДД Гл.5 П.45-46: Предупредительные сигналы (звуковые и световые) служат для предупреждения других участников движения об опасности. Аварийная сигнализация — одновременное мигание всех указателей поворота.",

"YHQ 6-bob 47-50-bandlar: Svetofor ishoralariga rioya qilish majburiy. Qizil — to\\'xtash, sariq — tayyorlanish, yashil — harakat.":
"ПДД Гл.6 П.47-50: Соблюдение сигналов светофора обязательно. Красный — стоять, жёлтый — приготовиться, зелёный — движение.",

"YHQ 6-bob 47-band: Yashil chiroq yonsa — harakatlanish ruxsat etiladi. Yashil chiroq miltillasa — harakatlanishga ruxsat etiladi, lekin signal o\\'chishiga tayyor bo\\'lish kerak.":
"ПДД Гл.6 П.47: Зелёный сигнал — движение разрешено. Мигающий зелёный — движение разрешено, но нужно быть готовым к смене сигнала.",

"YHQ 6-bob 48-band: Sariq chiroq miltillaganda: to\\'xtash chizig\\'i bo\\'lsa — to\\'xtash shart emas, lekin ehtiyotkor bo\\'lish kerak. Bu tartibga solinmagan chorrahani bildiradi.":
"ПДД Гл.6 П.48: При мигании жёлтого сигнала: при наличии стоп-линии — остановка не обязательна, но необходима осторожность. Это означает нерегулируемый перекрёсток.",

"YHQ 6-bob 48-band: Sariq chiroq yoqilganda to\\'xtash lozim (qizil hisoblanadi), lekin xavfsiz to\\'xtash imkoni bo\\'lmasa, chorrahani tezlikni oshirmay kesib o\\'tishga ruxsat etiladi.":
"ПДД Гл.6 П.48: При жёлтом сигнале следует остановиться (приравнивается к красному), но если безопасная остановка невозможна — разрешается проехать перекрёсток без увеличения скорости.",

"YHQ 6-bob 51-band: Tartiblashtiruvchi (militsioner yoki o\\'quvchi) ishoralariga svetofor ishoralaridan ustun rioya qilish shart. Uning ishoralari belgi va chiziqlardan ustun turadi.":
"ПДД Гл.6 П.51: Сигналы регулировщика (сотрудника полиции или дружинника) имеют приоритет над сигналами светофора. Его сигналы имеют приоритет перед знаками и разметкой.",

"YHQ 6-bob 51-band: Tartiblashtiruvchi qo\\'lini yuqoriga ko\\'tarsa — barcha yo\\'nalishlarda harakatlanish taqiqlanadi (sariq chiroqqa teng). Barchasi to\\'xtashi shart.":
"ПДД Гл.6 П.51: Если регулировщик поднимает руку вверх — движение во всех направлениях запрещено (равнозначно жёлтому сигналу). Все обязаны остановиться.",

"YHQ 7-bob 52-band: Belgilangan marshrut bo\\'ylab harakatlanuvchi transport vositalari (avtobus, trolleybus, tramvay) maxsus yo\\'laklarda harakatlanishi va boshqa transport vositalaridan ustunlikka ega bo\\'lishi mumkin.":
"ПДД Гл.7 П.52: Транспортные средства, движущиеся по установленному маршруту (автобус, троллейбус, трамвай), могут двигаться по специальным полосам и иметь преимущество перед другими ТС.",

"YHQ 7-bob 52-band: Maxsus transport vositalari (tez yordam, o\\'t o\\'chirish, militsiya, harbiy) ko\\'k yoki qizil miltillovchi chiroq va sirenani yoqib, ustunlikka ega. Boshqa transport vositalari yo\\'l berishlari shart.":
"ПДД Гл.7 П.52: Специальные ТС (скорая помощь, пожарная охрана, полиция, военные) с включёнными проблесковыми маячками и сиреной имеют преимущество. Другие ТС обязаны уступить дорогу.",

"YHQ 7-bob 52-band: Yo\\'nalishli transport vositalari (marshrut asosida ishlaydigan) uchun maxsus yo\\'laklar ajratilishi mumkin. Bu yo\\'laklarda boshqa transport vositalari harakatlanishi taqiqlanadi.":
"ПДД Гл.7 П.52: Для маршрутных транспортных средств могут выделяться специальные полосы. Движение других транспортных средств по этим полосам запрещено.",

"YHQ 8-bob 53-58-bandlar: Harakatlanishni boshlash, to\\'xtash, qaytib olish va yo\\'lakni o\\'zgartirish oldidan burilish chiroqlarini yoqish va boshqa transport vositalariga xalaqit bermaslik shart.":
"ПДД Гл.8 П.53-58: Перед началом движения, остановкой, разворотом и перестроением необходимо включать указатели поворота и не создавать помех другим ТС.",

"YHQ 8-bob 56-band: Harakatlanishni taqiqlovchi joylar: piyodalar o\\'tish joyi, yo\\'l to\\'sig\\'i oldida, tunnelda, ko\\'prikda, temir yo\\'l kesishmasida U-burilish va orqaga yurish taqiqlanadi.":
"ПДД Гл.8 П.56: Места, где запрещено движение: на пешеходном переходе, перед барьером, в тоннеле, на мосту, на железнодорожном переезде разворот и движение задним ходом запрещены.",

"YHQ 8-bob 57-band: Orqaga yurish: zarurat bo\\'lganda va boshqa transport vositalariga xalaqit bermasdan ruxsat etiladi. Chorrahada, piyodalar o\\'tish joyida, avtomagistrallarda taqiqlanadi.":
"ПДД Гл.8 П.57: Движение задним ходом: разрешено при необходимости без создания помех другим ТС. Запрещено на перекрёстках, пешеходных переходах и автомагистралях.",

"YHQ 9-bob 59-62-bandlar: Transport vositasi qatnov qismining o\\'ng tomonida, belgilangan yo\\'lakda harakatlanishi kerak. Chap yo\\'lakni faqat quvib o\\'tish uchun ishlatishga ruxsat etiladi.":
"ПДД Гл.9 П.59-62: ТС должно двигаться по правой стороне проезжей части в обозначенной полосе. Левая полоса разрешена только для обгона.",

"YHQ 9-bob 59-band: Transport vositalari qatnov qismining o\\'ng tomonida harakatlanishlari shart. O\\'ng tomonni egallagan holda harakatlanish chegarasi belgilangan. Chap yo\\'lak quvib o\\'tish uchun.":
"ПДД Гл.9 П.59: ТС должны двигаться по правой стороне проезжей части. Определены ограничения для занятия правой стороны. Левая полоса — для обгона.",

"YHQ 9-bob 60-band: Tramvay yo\\'li qatnov qismining markazida joylashgan bo\\'lsa, ko\\'chadan o\\'tishga ruxsat etiladi. Tramvay harakati ustuvor.":
"ПДД Гл.9 П.60: Если трамвайные пути расположены в центре проезжей части, движение по ним разрешено. Трамвай имеет приоритет.",

"YHQ Ilova 1, 1-bo\\'lim: 1.15 belgisi — sirpanchiq yo\\'l, 1.16 — yo\\'l qoplamasi g\\'adir-budurligi. Bu belgilar ko\\'rilganda tezlikni kamaytirish va ehtiyotkorlik qilish shart.":
"ПДД Приложение 1, Раздел 1: Знак 1.15 — скользкая дорога, 1.16 — неровная дорога. При виде этих знаков необходимо снизить скорость и соблюдать осторожность.",

"YHQ Ilova 1, 1-bo\\'lim: 1.1 va 1.2 belgilari — bir yoki ikki yo\\'lli temir yo\\'l kesishmasidan oldin o\\'rnatiladi. Belgidan 50-100 m (shahar ichida) yoki 150-300 m (shahardan tashqarida) oldin qo\\'yiladi.":
"ПДД Приложение 1, Раздел 1: Знаки 1.1 и 1.2 — устанавливаются перед одно- или двухпутным железнодорожным переездом. За 50–100 м (в городе) или 150–300 м (за городом) до переезда.",

"YHQ Ilova 1, 1-bo\\'lim: 1.22 belgisi — piyodalar o\\'tish joyi ogohlantiruvi. Bu belgidan keyin piyodalar o\\'tish joyi bor, ehtiyotkor bo\\'lish va tezlikni kamaytirish lozim.":
"ПДД Приложение 1, Раздел 1: Знак 1.22 — предупреждение о пешеходном переходе. После этого знака есть переход, необходимо соблюдать осторожность и снизить скорость.",

"YHQ Ilova 1, 1-bo\\'lim: 1.23 belgisi — maktab va maktabgacha ta\\'lim muassasalari yaqinida bolalar o\\'tishi haqida ogohlantirib, tezlikni 30 km/soatgacha kamaytirish tavsiya etiladi.":
"ПДД Приложение 1, Раздел 1: Знак 1.23 — предупреждение о переходе детей вблизи школ и дошкольных учреждений, рекомендуется снизить скорость до 30 км/ч.",

"YHQ Ilova 1, 1-bo\\'lim: Ogohlantiruv belgilari xavfli joylarda o\\'rnatiladi. Bu belgilar ko\\'rilganda tezlikni kamaytirish va yo\\'l sharoitiga diqqat qilish kerak.":
"ПДД Приложение 1, Раздел 1: Предупреждающие знаки устанавливаются в опасных местах. При виде этих знаков необходимо снизить скорость и обратить внимание на дорожную обстановку.",

"YHQ Ilova 1, 2-bo\\'lim: 2.1 belgisi — asosiy yo\\'l belgisi. Bu yo\\'lda harakatlanuvchi transport vositasi ikkinchi darajali yo\\'ldagi transport vositalaridan ustunlikka ega. 2.4 — yo\\'l bering, 2.5 — to\\'xtash majburiy.":
"ПДД Приложение 1, Раздел 2: Знак 2.1 — главная дорога. ТС, движущееся по главной дороге, имеет преимущество перед ТС на второстепенной. Знак 2.4 — уступите дорогу, 2.5 — движение без остановки запрещено.",

"YHQ Ilova 1, 2-bo\\'lim: Imtiyoz belgilari (2.1-2.7) transport vositalari harakatlanish tartibidagi ustunlikni belgilaydi. Asosiy yo\\'l belgilari yo\\'l kesishmasida kim avval o\\'tishini aniqlab beradi.":
"ПДД Приложение 1, Раздел 2: Знаки приоритета (2.1–2.7) определяют очерёдность движения ТС. Знаки главной дороги указывают, кто проезжает перекрёсток первым.",

"YHQ Ilova 1, 3-bo\\'lim: 3.20 — quvib o\\'tish taqiqlangan belgisi. 3.22 — yuk avtomobillarga quvib o\\'tish taqiqlangan. Keyingi chorrahagacha yoki ko\\'rsatilgan masofagacha amal qiladi.":
"ПДД Приложение 1, Раздел 3: Знак 3.20 — обгон запрещён. Знак 3.22 — обгон запрещён для грузовых автомобилей. Действует до ближайшего перекрёстка или на указанном расстоянии.",

"YHQ Ilova 1, 3-bo\\'lim: 3.27 — to\\'xtash taqiqlangan belgisi. 3.28 — to\\'xtab turish taqiqlangan belgisi. Belgi o\\'rnatilgan tomondagi yo\\'lning o\\'ng tomonida amal qiladi.":
"ПДД Приложение 1, Раздел 3: Знак 3.27 — остановка запрещена. Знак 3.28 — стоянка запрещена. Действует на правой стороне дороги со стороны установки знака.",

"YHQ Ilova 1, 3-bo\\'lim: Taqiqlovchi belgilar (3.1-3.34) harakatlanishni cheklovchi yoki taqiqlovchi ishoralar beradi. Bu belgilarga rioya qilish barcha haydovchilar uchun majburiydir.":
"ПДД Приложение 1, Раздел 3: Запрещающие знаки (3.1–3.34) вводят ограничения или запреты движения. Соблюдение этих знаков обязательно для всех водителей.",

"YHQ Ilova 1, 4-bo\\'lim: 4.6 belgisi — minimal tezlik chegarasi. Transport vositasi ko\\'rsatilgan tezlikdan sekin harakatlanishi mumkin emas. Avtomagistrallarda eng kam tezlik 40 km/soat.":
"ПДД Приложение 1, Раздел 4: Знак 4.6 — минимальная скорость. ТС не может двигаться медленнее указанной скорости. На автомагистралях минимальная скорость — 40 км/ч.",

"YHQ Ilova 1, 4-bo\\'lim: Buyuruv belgilari (4.1-4.9) harakatlanish yo\\'nalishini belgilaydi. 4.1 — to\\'g\\'ri yuring, 4.2 — o\\'ngga, 4.3 — chapga, 4.4 — to\\'g\\'ri yoki o\\'ngga, 4.5 — to\\'g\\'ri yoki chapga.":
"ПДД Приложение 1, Раздел 4: Предписывающие знаки (4.1–4.9) указывают направление движения. 4.1 — прямо, 4.2 — направо, 4.3 — налево, 4.4 — прямо или направо, 4.5 — прямо или налево.",

"YHQ Ilova 1, 4-bo\\'lim: Buyuruv belgilari (4.1-4.9) ma\\'lum yo\\'nalishlarda harakatlanishni majburiy qiladi. Bu belgilarga rioya etmaslik YHQ buzilishi hisoblanadi.":
"ПДД Приложение 1, Раздел 4: Предписывающие знаки (4.1–4.9) обязывают двигаться в определённых направлениях. Несоблюдение этих знаков является нарушением ПДД.",

"YHQ Ilova 1, 5-bo\\'lim: 5.19 belgisi — piyodalar o\\'tish joyi. Bu belgida piyodalar transport vositalariga yo\\'l bermasdan o\\'tishlari mumkin. Haydovchilar piyodalarga yo\\'l berishlari shart.":
"ПДД Приложение 1, Раздел 5: Знак 5.19 — пешеходный переход. На этом переходе пешеходы могут переходить без уступки дороги ТС. Водители обязаны уступить дорогу пешеходам.",

"YHQ Ilova 1, 5-bo\\'lim: 5.1 belgisi — avtomagistral boshi. Bu yo\\'lda tezlik 110 km/soatgacha, harakatlanish faqat tezkor yo\\'laklarda, piyoda va velosiped harakati taqiqlangan.":
"ПДД Приложение 1, Раздел 5: Знак 5.1 — начало автомагистрали. Скорость до 110 км/ч, движение только по скоростным полосам, движение пешеходов и велосипедистов запрещено.",

"YHQ Ilova 1, 5-bo\\'lim: Axborot-ko\\'rsatgich belgilari (5.1-5.33) yo\\'l sharoiti, xizmat joylari va boshqa muhim ma\\'lumotlar haqida xabar beradi.":
"ПДД Приложение 1, Раздел 5: Информационно-указательные знаки (5.1–5.33) информируют о дорожной обстановке, объектах сервиса и другой важной информации.",

"YHQ Ilova 1, 6-bo\\'lim: Servis belgilari (6.1-6.22) haydovchilarni yo\\'l bo\\'yidagi xizmat joylari: yonilg\\'i quyish stantsiyasi, ta\\'mirlash, ovqatlanish, mehmonxona va boshqa xizmatlar haqida xabardor qiladi.":
"ПДД Приложение 1, Раздел 6: Знаки сервиса (6.1–6.22) информируют водителей об объектах придорожного сервиса: АЗС, техобслуживания, питания, гостиниц и других услугах.",

"YHQ Ilova 1, 8-bo\\'lim: Qo\\'shimcha axborot (7.1.1-7.20) belgilari asosiy belgining ta\\'sir masofasini, yo\\'nalishini yoki tarqaladigan transport vositalari turini aniqlashtiradi. U asosiy belgining pastiga o\\'rnatiladi.":
"ПДД Приложение 1, Раздел 8: Знаки дополнительной информации (7.1.1–7.20) уточняют зону действия основного знака, направление или категорию ТС. Устанавливаются под основным знаком.",

"YHQ Ilova 2, 1.12-chiziq: To\\'xtash chizig\\'i (stop-chiziq). Svetofor yoki tartiblashtiruvchi ishorasida bu chiziqdan oldinda to\\'xtash shart. Chiziqdan o\\'tib to\\'xtash qoidabuzarlik.":
"ПДД Приложение 2, Разметка 1.12: Стоп-линия. По сигналу светофора или регулировщика необходимо остановиться перед этой линией. Остановка за линией является нарушением ПДД.",

"YHQ Ilova 2, 1.14-chiziq: Piyodalar o\\'tish joyi (zebra). Bu chiziqda haydovchilar piyodalarga yo\\'l berishlari shart. Haydovchi tezlikni kamaytirishi va piyodalar o\\'tishini kutishi kerak.":
"ПДД Приложение 2, Разметка 1.14: Пешеходный переход (зебра). На этой разметке водители обязаны уступать дорогу пешеходам. Необходимо снизить скорость и дождаться, пока пешеходы перейдут.",

"YHQ Ilova 2, 1.1-chiziq: Qattiq (uzilmagan) chiziqni kesib o\\'tish taqiqlanadi. Bu chiziq harakatlanish yo\\'nalishlarini ajratib, agar kesib o\\'tilsa — YHQ qoidabuzarlik hisoblanadi.":
"ПДД Приложение 2, Разметка 1.1: Сплошную линию пересекать запрещено. Эта линия разделяет направления движения, её пересечение является нарушением ПДД.",

"YHQ Ilova 2, 1.5-1.11-chiziqlar: Uzilgan chiziqlarni kesib o\\'tishga ruxsat etiladi. Quvib o\\'tish yoki qayta yo\\'lakka o\\'tish paytida uzilgan chiziqlardan o\\'tish mumkin.":
"ПДД Приложение 2, Разметка 1.5–1.11: Прерывистые линии разрешено пересекать. При обгоне или смене полосы движения можно пересекать прерывистые линии.",

"YHQ Ilova 2, 1.5-1.9-chiziqlar: Yo\\'lak chiziqlari harakatlanish yo\\'laklarini ajratadi. Uzilgan chiziq bo\\'lsa — kesib o\\'tishga ruxsat, qattiq chiziq bo\\'lsa — taqiqlanadi.":
"ПДД Приложение 2, Разметка 1.5–1.9: Линии полос разграничивают полосы движения. Прерывистая линия — пересечение разрешено, сплошная линия — запрещено.",

"YHQ Ilova 2, 2-bo\\'lim: Tik (vertikal) chiziqlar yo\\'l inshootlari va to\\'siqlarni belgilash uchun ishlatiladi. Oq-qora chiziqli panel (2.1-2.5) to\\'siq yoki xavfli joyni ko\\'rsatadi.":
"ПДД Приложение 2, Раздел 2: Вертикальная разметка применяется для обозначения дорожных сооружений и препятствий. Чёрно-белая полосатая панель (2.1–2.5) указывает на препятствие или опасное место.",

"YHQ Ilova 2: Yo\\'l chiziqlari (1.1-1.27) yo\\'l harakatini tartibga soladi. Uzilmagan (qattiq) chiziq — o\\'tib bo\\'lmaydi, uzilgan chiziq — o\\'tishga ruxsat. Chiziq rangiga va turiga qarab amal qilish lozim.":
"ПДД Приложение 2: Дорожная разметка (1.1–1.27) регулирует дорожное движение. Сплошная линия — пересечение запрещено, прерывистая — разрешено. Необходимо соблюдать с учётом цвета и вида линии.",

"YHQ Ilova 8: Old shisha — yoriqsiz, qoraltirilgan bo\\'lmasligi, tozaligini ta\\'minlovchi stekloochistitel ishlashi shart. Ko\\'rinishga xalaqit beradigan shikast bo\\'lsa — harakatlanish taqiqlanadi.":
"ПДД Приложение 8: Лобовое стекло — без трещин, не должно быть затонировано, стеклоочиститель должен работать. При наличии повреждений, мешающих обзору, — движение запрещено.",

"YHQ Ilova 8: Rulevoy boshqaruv tizimi buzilganda harakatlanish mutlaqo taqiqlanadi. Rulevoy o\\'zini-o\\'zi sezgir harakatlantirsa, bo\\'sh yurishda 10 gradusdan oshsa — taqiqlanadi.":
"ПДД Приложение 8: При неисправности рулевого управления движение категорически запрещено. Если руль самопроизвольно перемещается или люфт на холостом ходу превышает 10 градусов — запрещено.",

"YHQ Ilova 8: Shinalar holati: yengil avtomobil uchun protektor chuqurligi 1,6 mm dan, yuk — 1,0 mm dan, mototsikl — 0,8 mm dan kam bo\\'lmasligi shart. Shikastlangan shina bilan harakatlanish taqiqlanadi.":
"ПДД Приложение 8: Состояние шин: глубина протектора для легковых автомобилей — не менее 1,6 мм, грузовых — 1,0 мм, мотоциклов — 0,8 мм. Движение с повреждёнными шинами запрещено.",

"YHQ Ilova 8: Tormoz tizimi buzilgan transport vositasini haydash taqiqlanadi. Xizmat tormozi, qo\\'l tormozi va zaxira tormoz ishlashi shart. Bitta tizim ishlamasa — harakatlanish taqiqlanadi.":
"ПДД Приложение 8: Управление ТС с неисправной тормозной системой запрещено. Рабочий, стояночный и запасной тормоз должны работать. При неисправности одной системы — движение запрещено.",

"YHQ qoidalariga ko\\'ra, yo\\'l harakati bo\\'yicha belgilangan tartibga rioya qilish barcha yo\\'l harakati ishtirokchilari uchun majburiydir.":
"Согласно ПДД, соблюдение установленного порядка дорожного движения обязательно для всех участников дорожного движения.",
}

def escape_sql(s):
    return s.replace("'", "\\'")

def main():
    src = '/home/umidjon/Desktop/add_explanations.sql'
    out_ru  = '/home/umidjon/Desktop/add_explanations_RU.sql'
    out_uzk = '/home/umidjon/Desktop/add_explanations_UZK.sql'

    pattern = re.compile(r"UPDATE `Questions` SET `ExplanationUZ` = '((?:[^'\\]|\\.)*)' WHERE `Id` = (\d+);")

    lines_ru  = ["-- Add ExplanationRU column\n",
                 "ALTER TABLE `Questions` ADD COLUMN IF NOT EXISTS `ExplanationRU` LONGTEXT NULL;\n\n",
                 "START TRANSACTION;\n\n"]
    lines_uzk = ["-- Add ExplanationUZK column\n",
                 "ALTER TABLE `Questions` ADD COLUMN IF NOT EXISTS `ExplanationUZK` LONGTEXT NULL;\n\n",
                 "START TRANSACTION;\n\n"]

    missing = set()
    with open(src, encoding='utf-8') as f:
        for line in f:
            m = pattern.search(line)
            if not m:
                continue
            uz_text = m.group(1)
            qid = m.group(2)

            ru_text = RU.get(uz_text)
            if ru_text is None:
                missing.add(uz_text[:60])
                ru_text = "[Tarjima mavjud emas]"

            uzk_text = uz_to_cyrillic(uz_text.replace("\\'", "'"))
            uzk_text = uzk_text.replace("'", "\\'")
            ru_escaped  = escape_sql(ru_text)

            lines_ru.append(f"UPDATE `Questions` SET `ExplanationRU` = '{ru_escaped}' WHERE `Id` = {qid};\n")
            lines_uzk.append(f"UPDATE `Questions` SET `ExplanationUZK` = '{uzk_text}' WHERE `Id` = {qid};\n")

    lines_ru.append("\nCOMMIT;\n")
    lines_uzk.append("\nCOMMIT;\n")

    with open(out_ru,  'w', encoding='utf-8') as f: f.writelines(lines_ru)
    with open(out_uzk, 'w', encoding='utf-8') as f: f.writelines(lines_uzk)

    print(f"✓ {out_ru}")
    print(f"✓ {out_uzk}")
    if missing:
        print(f"\n⚠ {len(missing)} noyob matnlar topilmadi (qisman ko'rsatilgan):")
        for t in sorted(missing): print(" -", t)

if __name__ == '__main__':
    main()
