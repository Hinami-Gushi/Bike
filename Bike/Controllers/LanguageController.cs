using Microsoft.AspNetCore.Mvc;

namespace Bike.Controllers
{
    public class LanguageController : Controller
    {
        public IActionResult Set(string lang)
        {
            HttpContext.Session.SetString("lang", lang);

            string currency = lang switch
            {
                "ja" => "JPY",
                "vi" => "VND",
                _ => "USD"
            };
            HttpContext.Session.SetString("currency", currency);

            return Redirect(Request.Headers["Referer"].ToString());
        }
    }
}