using Microsoft.AspNetCore.Mvc;

namespace Bike.Controllers
{
    public class LanguageController : Controller
    {
        public IActionResult Set(string lang)
        {
            HttpContext.Session.SetString("lang", lang);

            return Redirect(Request.Headers["Referer"].ToString());
        }
    }
}