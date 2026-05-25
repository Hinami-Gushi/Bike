using Bike.Data;
using Bike.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Bike.Controllers
{
    public class HomeController : Controller
    {
        private readonly BikeDbContext _context;

        public HomeController(BikeDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var logs = _context.FuelLogs.ToList() ?? new List<FuelLog>();
            return View(logs);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
