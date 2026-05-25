using Microsoft.AspNetCore.Mvc;
using Bike.Data;
using Bike.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;

namespace Bike.Controllers
{
    public class AccountController : Controller
    {
        private readonly BikeDbContext _context;

        public AccountController(BikeDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string email, string password)
        {
            if (_context.Users.Any(u => u.Email == email))
            {
                ViewBag.ErrorKey = "ErrEmailRegistered";
                return View();
            }

            var user = new User
            {
                Email = email,
                PasswordHash = HashPassword(password)
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var hash = HashPassword(password);
            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.PasswordHash == hash);

            if (user == null)
            {
                ViewBag.ErrorKey = "ErrInvalidAuth";
                return View();
            }

            HttpContext.Session.SetInt32("userId", user.Id);
            HttpContext.Session.SetString("userEmail", user.Email);

            return RedirectToAction("Dashboard", "FuelLogs");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}