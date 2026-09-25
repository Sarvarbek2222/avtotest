using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using propro.Localization;

namespace propro.Controllers
{
    [AllowAnonymous] // til login oynasida ham tanlanadi
    public class LangController : Controller
    {
        /// <summary>
        /// Tilni tanlash (JS o'chiq bo'lsa ham ishlaydi). Til standart .AspNetCore.Culture cookie'sida 1 yil saqlanadi.
        /// /Lang/Set?lang=ru&amp;returnUrl=/Topic/Index
        /// </summary>
        [HttpGet]
        public IActionResult Set(string lang, string? returnUrl)
        {
            Lang.SetCookie(Response, lang);
            Response.Cookies.Delete(Lang.LegacyCookieName); // eski versiya cookie'si

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        /// <summary>Brauzer uchun lug'at: window.I18N_TEXTS = {uz:{...}, ru:{...}, uzk:{...}}</summary>
        [HttpGet]
        [ResponseCache(Duration = 86400)]
        public IActionResult Texts()
        {
            var js = "window.I18N_TEXTS=" + Localization.Texts.ToJson() + ";";
            return Content(js, "application/javascript; charset=utf-8");
        }
    }
}
