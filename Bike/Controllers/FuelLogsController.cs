using Microsoft.AspNetCore.Mvc;
using Bike.Data;
using Bike.Models;

namespace Bike.Controllers
{
    public class FuelLogsController : Controller
    {
        private readonly BikeDbContext _context;

        public FuelLogsController(BikeDbContext context)
        {
            _context = context;
        }

        // 一覧
        public IActionResult Index()
        {
            var logs = _context.FuelLogs.ToList();
            return View(logs);
        }

        // Dashboard
        public IActionResult Dashboard()
        {
            var logs = _context.FuelLogs.ToList();

            double averageFuelEfficiency = logs.Any()
                ? logs.Average(x => x.FuelEfficiency)
                : 0;

            double monthlyFuelCost = logs
                .Where(x => x.FuelDate.Month == DateTime.Now.Month)
                .Sum(x => x.Cost ?? 0);

            ViewBag.AverageFuelEfficiency = averageFuelEfficiency;
            ViewBag.MonthlyFuelCost = monthlyFuelCost;

            return View(logs);
        }

        // 追加画面
        public IActionResult Create()
        {
            return View();
        }

        // 保存
        [HttpPost]
        public IActionResult Create(FuelLog log)
        {
            log.CreatedAt = DateTime.Now;

            _context.FuelLogs.Add(log);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}