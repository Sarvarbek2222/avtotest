using System.Text;

namespace propro.Localization
{
    /// <summary>
    /// O'zbek lotin ⇄ kirill transliteratsiyasi.
    /// wwwroot/js/i18n.js ichidagi toCyrillic() shu qoidalarning JS nusxasi — birini o'zgartirsangiz, ikkinchisini ham yangilang.
    /// </summary>
    public static class Translit
    {
        // Lotin yozuvida tutuq belgisi va o'/g' uchun ishlatiladigan barcha apostrof variantlari
        private const string Apostrophes = "'`ʻʼ‘’´";

        private static bool IsApos(char c) => Apostrophes.IndexOf(c) >= 0;

        private static bool IsLatinLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

        private static bool IsLatinVowel(char c) => "aeiouAEIOU".IndexOf(c) >= 0;

        private static readonly Dictionary<char, string> LatSingle = new()
        {
            ['a'] = "а", ['b'] = "б", ['c'] = "с", ['d'] = "д", ['f'] = "ф", ['g'] = "г", ['h'] = "ҳ",
            ['i'] = "и", ['j'] = "ж", ['k'] = "к", ['l'] = "л", ['m'] = "м", ['n'] = "н", ['o'] = "о",
            ['p'] = "п", ['q'] = "қ", ['r'] = "р", ['s'] = "с", ['t'] = "т", ['u'] = "у", ['v'] = "в",
            ['w'] = "в", ['x'] = "х", ['y'] = "й", ['z'] = "з",
        };

        /// <summary>O'zbek lotin matnini kirill yozuviga o'giradi.</summary>
        public static string ToCyrillic(string? text)
        {
            if (string.IsNullOrEmpty(text)) return text ?? "";

            var sb = new StringBuilder(text.Length);
            int n = text.Length;

            for (int i = 0; i < n; i++)
            {
                char c = text[i];

                if (!IsLatinLetter(c))
                {
                    // Tutuq belgisi: harfdan keyin kelsa → ъ (ma'no → маъно)
                    if (IsApos(c) && i > 0 && IsLatinLetter(text[i - 1]) && i + 1 < n && IsLatinLetter(text[i + 1]))
                    {
                        // s'h, c'h → сҳ (belgi faqat ajratuvchi)
                        char p = char.ToLowerInvariant(text[i - 1]);
                        if ((p == 's' || p == 'c') && char.ToLowerInvariant(text[i + 1]) == 'h') continue;
                        sb.Append(char.IsUpper(text[i - 1]) && char.IsUpper(text[i + 1]) ? 'Ъ' : 'ъ');
                        continue;
                    }
                    sb.Append(c);
                    continue;
                }

                char lc = char.ToLowerInvariant(c);
                bool upper = char.IsUpper(c);
                char next = i + 1 < n ? text[i + 1] : '\0';
                char nl = char.ToLowerInvariant(next);
                // Keyingi harf ham katta bo'lsa (yoki so'z tugasa) — butun so'z katta harfda deb hisoblaymiz
                bool nextUpper = char.IsUpper(next);
                bool wordStart = i == 0 || !IsLatinLetter(text[i - 1]) && !IsApos(text[i - 1]);

                string? outStr = null;
                int consumed = 1;

                // o' va g'
                if ((lc == 'o' || lc == 'g') && IsApos(next))
                {
                    outStr = lc == 'o' ? "ў" : "ғ";
                    consumed = 2;
                }
                else if (lc == 's' && nl == 'h') { outStr = "ш"; consumed = 2; }
                else if (lc == 'c' && nl == 'h') { outStr = "ч"; consumed = 2; }
                else if (lc == 'y' && (nl == 'o' || nl == 'u' || nl == 'a' || nl == 'e') && !IsApos(i + 2 < n ? text[i + 2] : '\0'))
                {
                    outStr = nl switch { 'o' => "ё", 'u' => "ю", 'a' => "я", _ => "е" };
                    consumed = 2;
                }
                else if (lc == 't' && nl == 's' && EndsWithSuffix(text, i + 2, "iya", "iyal", "ion", "io"))
                {
                    // politsiya → полиция, funktsional → функционал
                    outStr = "ц";
                    consumed = 2;
                }
                else if (lc == 'e')
                {
                    bool afterVowel = i > 0 && IsLatinVowel(text[i - 1]);
                    outStr = wordStart || afterVowel ? "э" : "е";
                }
                else
                {
                    outStr = LatSingle[lc];
                }

                if (upper)
                {
                    // "SH", "Sh" → "Ш"; butun so'z katta bo'lsa hammasi katta
                    outStr = outStr.ToUpperInvariant();
                }
                else if (consumed == 2 && nextUpper)
                {
                    outStr = outStr.ToUpperInvariant();
                }

                sb.Append(outStr);
                i += consumed - 1;
            }

            return sb.ToString();
        }

        private static bool EndsWithSuffix(string text, int pos, params string[] suffixes)
        {
            foreach (var s in suffixes)
            {
                if (pos + s.Length <= text.Length &&
                    string.Compare(text, pos, s, 0, s.Length, StringComparison.OrdinalIgnoreCase) == 0)
                    return true;
            }
            return false;
        }

        private static readonly Dictionary<char, string> CyrMap = new()
        {
            ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d", ['ё'] = "yo", ['ж'] = "j",
            ['з'] = "z", ['и'] = "i", ['й'] = "y", ['к'] = "k", ['л'] = "l", ['м'] = "m", ['н'] = "n",
            ['о'] = "o", ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t", ['у'] = "u", ['ф'] = "f",
            ['х'] = "x", ['ц'] = "ts", ['ч'] = "ch", ['ш'] = "sh", ['щ'] = "sh", ['ъ'] = "'", ['ы'] = "i",
            ['ь'] = "", ['э'] = "e", ['ю'] = "yu", ['я'] = "ya", ['ў'] = "o'", ['қ'] = "q", ['ғ'] = "g'",
            ['ҳ'] = "h",
        };

        private static bool IsCyrLetter(char c) => CyrMap.ContainsKey(char.ToLowerInvariant(c)) || char.ToLowerInvariant(c) == 'е';

        private static bool IsCyrVowel(char c) => "аеёиоуэюяўъь".IndexOf(char.ToLowerInvariant(c)) >= 0;

        /// <summary>O'zbek kirill matnini lotin yozuviga o'giradi.</summary>
        public static string ToLatin(string? text)
        {
            if (string.IsNullOrEmpty(text)) return text ?? "";

            var sb = new StringBuilder(text.Length + 8);
            int n = text.Length;

            for (int i = 0; i < n; i++)
            {
                char c = text[i];
                char lc = char.ToLowerInvariant(c);

                string? mapped;
                if (lc == 'е')
                {
                    bool wordStart = i == 0 || !IsCyrLetter(text[i - 1]);
                    bool afterVowel = i > 0 && IsCyrVowel(text[i - 1]);
                    mapped = wordStart || afterVowel ? "ye" : "e";
                }
                else if (!CyrMap.TryGetValue(lc, out mapped))
                {
                    sb.Append(c);
                    continue;
                }

                if (char.IsUpper(c) && mapped.Length > 0)
                {
                    char next = i + 1 < n ? text[i + 1] : '\0';
                    bool wholeUpper = char.IsUpper(next) || (i > 0 && char.IsUpper(text[i - 1]) && !char.IsLetter(next));
                    mapped = wholeUpper
                        ? mapped.ToUpperInvariant()
                        : char.ToUpperInvariant(mapped[0]) + mapped.Substring(1);
                }

                sb.Append(mapped);
            }

            return sb.ToString();
        }

        /// <summary>Matnda o'zbek/rus kirill harflari bormi.</summary>
        public static bool HasCyrillic(string? text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (var c in text)
                if (c >= 'Ѐ' && c <= 'ӿ') return true;
            return false;
        }

        /// <summary>Matnning asosiy qismi kirillda yozilganmi (lotin harflaridan ko'p).</summary>
        public static bool IsMostlyCyrillic(string? text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            int cyr = 0, lat = 0;
            foreach (var c in text)
            {
                if (c >= 'Ѐ' && c <= 'ӿ') cyr++;
                else if (IsLatinLetter(c)) lat++;
            }
            return cyr > lat;
        }
    }
}
