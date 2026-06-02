using Microsoft.AspNetCore.Mvc;
using Bike.Data;
using Bike.Models;
using System;
using System.Linq;

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
        public IActionResult Index(DateTime? startDate, DateTime? endDate)
        {
            var userId = HttpContext.Session.GetInt32("userId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var query = _context.FuelLogs.Where(x => x.UserId == userId);
            bool isSearchActive = startDate.HasValue || endDate.HasValue;

            if (startDate.HasValue)
            {
                var startUtc = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
                query = query.Where(x => x.FuelDate >= startUtc);
            }

            if (endDate.HasValue)
            {
                var endUtc = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
                query = query.Where(x => x.FuelDate <= endUtc);
            }

            // 検索条件がない場合は最新の10件を表示する
            if (!isSearchActive)
            {
                query = query.Take(10);
            }

            var logs = query
                .OrderByDescending(x => x.FuelDate)
                .ToList();

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.IsSearchActive = isSearchActive;

            return View(logs);
        }

        // Dashboard
        public IActionResult Dashboard()
        {
            var userId = HttpContext.Session.GetInt32("userId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var logs = _context.FuelLogs
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.FuelDate)
                .ToList();

            double averageFuelEfficiency = logs.Any()
                ? logs.Average(x => x.FuelEfficiency)
                : 0;

            double monthlyFuelCost = logs
                .Where(x => x.FuelDate.Year == DateTime.UtcNow.Year && x.FuelDate.Month == DateTime.UtcNow.Month)
                .Sum(x => x.Cost ?? 0);

            double totalFuel = logs.Sum(x => x.FuelLiter);
            double totalDistance = logs.Sum(x => x.DistanceKm);
            double latestEfficiency = logs.FirstOrDefault()?.FuelEfficiency ?? 0;

            ViewBag.AverageFuelEfficiency = averageFuelEfficiency;
            ViewBag.MonthlyFuelCost = monthlyFuelCost;
            ViewBag.TotalFuel = totalFuel;
            ViewBag.TotalDistance = totalDistance;
            ViewBag.LatestEfficiency = latestEfficiency;

            return View(logs);
        }

        // Monthly（画面枠）
        public IActionResult Monthly()
        {
            var userId = HttpContext.Session.GetInt32("userId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var logs = _context.FuelLogs
                .Where(x => x.UserId == userId && x.FuelDate.Year == DateTime.UtcNow.Year && x.FuelDate.Month == DateTime.UtcNow.Month)
                .ToList();

            double totalFuel = logs.Sum(x => x.FuelLiter);
            double totalDistance = logs.Sum(x => x.DistanceKm);
            double totalCost = logs.Sum(x => x.Cost ?? 0);
            double averageEfficiency = totalFuel > 0 ? totalDistance / totalFuel : 0;

            ViewBag.TotalFuel = totalFuel;
            ViewBag.TotalDistance = totalDistance;
            ViewBag.TotalCost = totalCost;
            ViewBag.AverageEfficiency = averageEfficiency;
            ViewBag.MonthYear = DateTime.UtcNow.ToString("MMMM yyyy");

            return View();
        }

        // 追加画面
        public IActionResult Create()
        {
            var userId = HttpContext.Session.GetInt32("userId");
            if (userId == null) return RedirectToAction("Login", "Account");

            return View(new FuelLog
            {
                FuelDate = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc)
            });
        }

        // 保存
        [HttpPost]
        public IActionResult Create(FuelLog log)
        {
            var userId = HttpContext.Session.GetInt32("userId");
            if (userId == null) return RedirectToAction("Login", "Account");

            if (log == null)
            {
                return BadRequest("Invalid data submitted.");
            }

            log.UserId = userId.Value;

            // Convert to UTC as Npgsql 6.0+ requires UTC for 'timestamp with time zone'
            log.FuelDate = DateTime.SpecifyKind(log.FuelDate, DateTimeKind.Utc);
            log.CreatedAt = DateTime.UtcNow;
            
            string sessionCurrency = HttpContext.Session.GetString("currency") ?? "USD";
            log.Currency ??= sessionCurrency;

            _context.FuelLogs.Add(log);
            _context.SaveChanges();

            return RedirectToAction("Dashboard");
        }

        // 編集画面
        public IActionResult Edit(int id)
        {
            var userId = HttpContext.Session.GetInt32("userId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var log = _context.FuelLogs.FirstOrDefault(x => x.Id == id && x.UserId == userId);
            if (log is null)
            {
                return NotFound();
            }

            return View(log);
        }

        // 更新
        [HttpPost]
        public IActionResult Edit(FuelLog log)
        {
            var userId = HttpContext.Session.GetInt32("userId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var existing = _context.FuelLogs.FirstOrDefault(x => x.Id == log.Id && x.UserId == userId);
            if (existing is null)
            {
                return NotFound();
            }

            existing.FuelDate = DateTime.SpecifyKind(log.FuelDate, DateTimeKind.Utc);
            existing.FuelLiter = log.FuelLiter;
            existing.DistanceKm = log.DistanceKm;
            existing.Cost = log.Cost;
            string sessionCurrency = HttpContext.Session.GetString("currency") ?? "USD";
            existing.Currency = string.IsNullOrWhiteSpace(log.Currency) ? existing.Currency ?? sessionCurrency : log.Currency;

            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        // 削除
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("userId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var log = _context.FuelLogs.FirstOrDefault(x => x.Id == id && x.UserId == userId);
            if (log is null)
            {
                return NotFound();
            }

            _context.FuelLogs.Remove(log);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}