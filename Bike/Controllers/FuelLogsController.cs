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

        public FuelLogsController(BikeDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
            _appId = configuration["EStat:AppId"] ?? "";
            _statsDataId = configuration["EStat:StatsDataId"] ?? "0003421913"; // 小売物価統計調査 (Regular Gasoline)
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

            // 検索結果の集計
            ViewBag.SearchTotalFuel = logs.Sum(x => x.FuelLiter);
            ViewBag.SearchTotalDistance = logs.Sum(x => x.DistanceKm);
            ViewBag.SearchTotalJPY = logs.Where(x => x.Currency == "JPY").Sum(x => x.Cost ?? 0);
            ViewBag.SearchTotalVND = logs.Where(x => x.Currency == "VND").Sum(x => x.Cost ?? 0);

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

            string selectedCurrency = HttpContext.Session.GetString("currency") ?? "USD";

            var allLogs = _context.FuelLogs
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.FuelDate)
                .ToList();

            var logs = allLogs.Where(x => x.Currency == selectedCurrency).ToList();

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

            return View(allLogs);
        }

        // Monthly（画面枠）
        public IActionResult Monthly()
        {
            var userId = HttpContext.Session.GetInt32("userId");
            if (userId == null) return RedirectToAction("Login", "Account");

            string selectedCurrency = HttpContext.Session.GetString("currency") ?? "USD";

            var logs = _context.FuelLogs
                .Where(x => x.UserId == userId && 
                            x.FuelDate.Year == DateTime.UtcNow.Year && 
                            x.FuelDate.Month == DateTime.UtcNow.Month &&
                            x.Currency == selectedCurrency)
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
        public async Task<IActionResult> Create(FuelLog log, string Region, double? Latitude, double? Longitude)
        {
            var userId = HttpContext.Session.GetInt32("userId");
            if (userId == null) return RedirectToAction("Login", "Account");


            if (log == null)
            {
                return BadRequest("Invalid data submitted.");
            }
            log.UserId = userId.Value;

            // --- 自動計算ロジック (ベトナム・日本特化) ---
            const double PRICE_VN = 23000.0; // 1L = 23,000 VND

            if (Region == "JP")
            {
                double priceJp = await GetJapanGasPriceAsync();
                log.Currency = "JPY";
                log.FuelLiter = (log.Cost ?? 0) / priceJp;
            }
            else // Default to VN
            {
                log.Currency = "VND";
                log.FuelLiter = (log.Cost ?? 0) / PRICE_VN;
            }

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

            log.FuelDate = DateTime.SpecifyKind(log.FuelDate, DateTimeKind.Utc);
            log.CreatedAt = DateTime.UtcNow;

            _context.FuelLogs.Add(log);
            _context.SaveChanges();

            return RedirectToAction("Dashboard");
        }

        private async Task<double> GetJapanGasPriceAsync()
        {
            try
            {
                // e-Stat API: 小売物価統計調査 (Regular Gasoline in Tokyo as proxy)
                // cdCat01=07301 (Gasoline), cdArea=13101 (Tokyo)
                string url = $"https://api.e-stat.go.jp/rest/3.0/app/json/getStatsData?appId={_appId}&statsDataId={_statsDataId}&cdCat01=07301&cdArea=13101";
                
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("GET_STATS_DATA", out var getData) &&
                        getData.TryGetProperty("STATISTICAL_DATA", out var statData) &&
                        statData.TryGetProperty("DATA_INF", out var dataInf) &&
                        dataInf.TryGetProperty("VALUE", out var values))
                    {
                        JsonElement latestEntry;
                        if (values.ValueKind == JsonValueKind.Array)
                        {
                            latestEntry = values.EnumerateArray().LastOrDefault();
                        }
                        else
                        {
                            latestEntry = values;
                        }

                        if (latestEntry.TryGetProperty("$", out var priceVal))
                        {
                            if (double.TryParse(priceVal.GetString(), out double price))
                            {
                                return price;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Japan gas price: {ex.Message}");
            }

            return 170.0; // Fallback value
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
