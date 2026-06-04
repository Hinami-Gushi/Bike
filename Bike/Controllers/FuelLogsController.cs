
using Microsoft.AspNetCore.Mvc;
using Bike.Data;
using Bike.Models;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Bike.Controllers
{
    public class FuelLogsController : Controller
    {
        private readonly BikeDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly string _appId;
        private readonly string _statsDataId;

        public FuelLogsController(
            BikeDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();

            _appId =
                configuration["EStat:AppId"] ?? "";

            _statsDataId =
                configuration["EStat:StatsDataId"]
                ?? "0003421913";
        }

        // =========================
        // INDEX
        // =========================
        public IActionResult Index(
            DateTime? startDate,
            DateTime? endDate)
        {
            var userId =
                HttpContext.Session.GetInt32("userId");

            if (userId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var query = _context.FuelLogs
                .Where(x => x.UserId == userId);

            bool isSearchActive =
                startDate.HasValue ||
                endDate.HasValue;

            if (startDate.HasValue)
            {
                var start =
                    DateTime.SpecifyKind(
                        startDate.Value,
                        DateTimeKind.Utc);

                query =
                    query.Where(
                        x => x.FuelDate >= start);
            }

            if (endDate.HasValue)
            {
                var end =
                    DateTime.SpecifyKind(
                        endDate.Value,
                        DateTimeKind.Utc);

                query =
                    query.Where(
                        x => x.FuelDate <= end);
            }

            if (!isSearchActive)
            {
                query = query
                    .OrderByDescending(x => x.FuelDate)
                    .Take(10);
            }
            else
            {
                query = query
                    .OrderByDescending(x => x.FuelDate);
            }

            var logs = query.ToList();

            ViewBag.SearchTotalFuel =
                logs.Sum(x => x.FuelLiter);

            ViewBag.SearchTotalDistance =
                logs.Sum(x => x.DistanceKm);

            ViewBag.SearchTotalJPY =
                logs.Where(x => x.Currency == "JPY")
                    .Sum(x => x.Cost ?? 0);

            ViewBag.SearchTotalVND =
                logs.Where(x => x.Currency == "VND")
                    .Sum(x => x.Cost ?? 0);

            ViewBag.StartDate =
                startDate?.ToString("yyyy-MM-dd");

            ViewBag.EndDate =
                endDate?.ToString("yyyy-MM-dd");

            ViewBag.IsSearchActive =
                isSearchActive;

            return View(logs);
        }

        // =========================
        // DASHBOARD
        // =========================
        public IActionResult Dashboard()
        {
            var userId =
                HttpContext.Session.GetInt32("userId");

            if (userId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var selectedCurrency =
                HttpContext.Session.GetString("currency")
                ?? "USD";

            var allLogs = _context.FuelLogs
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.FuelDate)
                .ToList();

            var logs = allLogs
                .Where(x =>
                    x.Currency == selectedCurrency ||
                    x.Currency == null)
                .ToList();

            double totalFuel =
                logs.Sum(x => x.FuelLiter);

            double totalDistance =
                logs.Sum(x => x.DistanceKm);

            double averageFuelEfficiency =
                totalFuel > 0
                    ? totalDistance / totalFuel
                    : 0;

            var now = DateTime.Now;

            double monthlyFuelCost = logs
                .Where(x =>
                    x.FuelDate.Year == now.Year &&
                    x.FuelDate.Month == now.Month)
                .Sum(x => x.Cost ?? 0);

            ViewBag.AverageFuelEfficiency =
                averageFuelEfficiency;

            ViewBag.MonthlyFuelCost =
                monthlyFuelCost;

            ViewBag.TotalFuel =
                totalFuel;

            ViewBag.TotalDistance =
                totalDistance;

            ViewBag.LatestEfficiency =
                logs.FirstOrDefault()?.FuelEfficiency
                ?? 0;

            return View(allLogs);
        }

        // =========================
        // MONTHLY
        // =========================
        public IActionResult Monthly()
        {
            var userId =
                HttpContext.Session.GetInt32("userId");

            if (userId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var selectedCurrency =
                HttpContext.Session.GetString("currency")
                ?? "USD";

            var fourMonthsAgo =
                DateTime.UtcNow.AddMonths(-3);

            var startDate = new DateTime(
                fourMonthsAgo.Year,
                fourMonthsAgo.Month,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc);

            var logs = _context.FuelLogs
                .Where(x => x.UserId == userId &&
                            x.FuelDate >= startDate &&
                            x.Currency == selectedCurrency)
                .Where(x =>
                    x.UserId == userId &&
                    x.FuelDate >= startDate &&
                    x.Currency == selectedCurrency)
                .ToList();

            string lang = HttpContext.Session.GetString("lang") ?? "en";

            var monthlySummaries = logs
                .GroupBy(x => new { x.FuelDate.Year, x.FuelDate.Month })
                .Select(g => new
                {
                    MonthYear = lang == "ja" 
                        ? $"{g.Key.Year}年{g.Key.Month}月" 
                        : $"{g.Key.Year}/{g.Key.Month:D2}",
                .GroupBy(x => new
                {
                    x.FuelDate.Year,
                    x.FuelDate.Month
                })
                .Select(g => new
                {
                    MonthYear = new DateTime(
                        g.Key.Year,
                        g.Key.Month,
                        1).ToString("MMMM yyyy"),

                    Year = g.Key.Year,

                    Month = g.Key.Month,

                    TotalFuel =
                        g.Sum(x => x.FuelLiter),

                    TotalDistance =
                        g.Sum(x => x.DistanceKm),

                    TotalCost =
                        g.Sum(x => x.Cost ?? 0),

                    AverageEfficiency =
                        g.Sum(x => x.FuelLiter) > 0
                            ? g.Sum(x => x.DistanceKm)
                                / g.Sum(x => x.FuelLiter)
                            : 0
                })
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToList();

            ViewBag.MonthlySummaries =
                monthlySummaries;

            return View();
        }

        // =========================
        // CREATE GET
        // =========================
        public IActionResult Create()
        {
            var userId =
                HttpContext.Session.GetInt32("userId");

            if (userId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            return View(new FuelLog
            {
                FuelDate = DateTime.SpecifyKind(
                    DateTime.Today,
                    DateTimeKind.Utc)
            });
        }

        // =========================
        // CREATE POST
        // =========================
        [HttpPost]
        public async Task<IActionResult> Create(
            FuelLog log,
            string Region,
            double? Latitude,
            double? Longitude)
        {
            var userId = HttpContext.Session.GetInt32("userId");
            if (userId == null) return RedirectToAction("Login", "Account");


            if (log == null)
            {
                return BadRequest("Invalid data submitted.");
            }


            log.UserId = userId.Value;

            // --- 自動計算ロジック (ベトナム・日本・アメリカ特化) ---
            const double PRICE_VN = 23000.0; // 1L = 23,000 VND
            const double PRICE_US = 0.95;    // 1L = 0.95 USD (approx)
            var userId =
                HttpContext.Session.GetInt32("userId");

            if (userId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            if (log == null)
                return BadRequest("Invalid data");

            log.UserId = userId.Value;

            Region =
                (Region ?? "VN").ToUpper();

            const double PRICE_VN = 23000.0;

            if (Region == "JP")
            {
                var priceJp =
                    await GetJapanGasPriceAsync();

                log.Currency = "JPY";

                if (priceJp > 0)
                {
                    log.FuelLiter =
                        (log.Cost ?? 0) / priceJp;
                }
            }
            else if (Region == "US")
            {
                log.Currency = "USD";
                log.FuelLiter = (log.Cost ?? 0) / PRICE_US;
            }
            else // Default to VN
            else
            {
                log.Currency = "VND";

            // --- 走行距離（Distance km）の自動計算の準備 ---
            double currentLat = Latitude ?? 0;
            double currentLng = Longitude ?? 0;

            // TODO: ここでGoong MapsまたはGoogle Maps APIを呼び出し、距離を算出する
            log.DistanceKm = 0; 




            // UserId が 0 (初期値) の場合、暫定的に 1 をセットする（未ログイン等の場合）
            if (log.UserId == 0)
            {
                log.UserId = 1;
            }

            // Convert to UTC as Npgsql 6.0+ requires UTC for 'timestamp with time zone'

                log.FuelLiter =
                    (log.Cost ?? 0) / PRICE_VN;
            }

            log.DistanceKm = 0;

            log.FuelDate =
                DateTime.SpecifyKind(
                    log.FuelDate,
                    DateTimeKind.Utc);

            log.CreatedAt =
                DateTime.UtcNow;

            _context.FuelLogs.Add(log);

            await _context.SaveChangesAsync();

            return RedirectToAction("Dashboard");
        }

        // =========================
        // JAPAN GAS API
        // =========================
        private async Task<double> GetJapanGasPriceAsync()
        {
            try
            {
                string url =
                    $"https://api.e-stat.go.jp/rest/3.0/app/json/getStatsData" +
                    $"?appId={_appId}" +
                    $"&statsDataId={_statsDataId}" +
                    $"&cdCat01=07301&cdArea=13101";

                var res =
                    await _httpClient.GetAsync(url);

                if (!res.IsSuccessStatusCode)
                    return 170.0;

                var json =
                    await res.Content.ReadAsStringAsync();

                using var doc =
                    JsonDocument.Parse(json);

                var root =
                    doc.RootElement;

                if (root.TryGetProperty(
                        "GET_STATS_DATA",
                        out var g)
                    &&
                    g.TryGetProperty(
                        "STATISTICAL_DATA",
                        out var s)
                    &&
                    s.TryGetProperty(
                        "DATA_INF",
                        out var d)
                    &&
                    d.TryGetProperty(
                        "VALUE",
                        out var v))
                {
                    var latest =
                        v.ValueKind == JsonValueKind.Array
                            ? v.EnumerateArray()
                                .LastOrDefault()
                            : v;

                    if (latest.TryGetProperty(
                            "$",
                            out var price))
                    {
                        if (double.TryParse(
                                price.GetString(),
                                out var p))
                        {
                            return p;
                        }
                    }
                }
            }
            catch
            {
            }

            return 170.0;
        }

        // =========================
        // EDIT GET
        // =========================
        public IActionResult Edit(int id)
        {
            var userId =
                HttpContext.Session.GetInt32("userId");

            if (userId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var log = _context.FuelLogs
                .FirstOrDefault(x =>
                    x.Id == id &&
                    x.UserId == userId);

            if (log == null)
                return NotFound();

            return View(log);
        }

        // =========================
        // EDIT POST
        // =========================
        [HttpPost]
        public IActionResult Edit(FuelLog log)
        {
            var userId =
                HttpContext.Session.GetInt32("userId");

            if (userId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var existing = _context.FuelLogs
                .FirstOrDefault(x =>
                    x.Id == log.Id &&
                    x.UserId == userId);

            if (existing == null)
                return NotFound();

            existing.FuelDate =
                DateTime.SpecifyKind(
                    log.FuelDate,
                    DateTimeKind.Utc);

            existing.FuelLiter =
                log.FuelLiter;

            existing.DistanceKm =
                log.DistanceKm;

            existing.Cost =
                log.Cost;

            var sessionCurrency =
                HttpContext.Session.GetString("currency")
                ?? "USD";

            existing.Currency =
                log.Currency ?? sessionCurrency;

            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // =========================
        // DELETE
        // =========================
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var userId =
                HttpContext.Session.GetInt32("userId");

            if (userId == null)
                return RedirectToAction(
                    "Login",
                    "Account");

            var log = _context.FuelLogs
                .FirstOrDefault(x =>
                    x.Id == id &&
                    x.UserId == userId);

            if (log == null)
                return NotFound();

            _context.FuelLogs.Remove(log);

            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}
