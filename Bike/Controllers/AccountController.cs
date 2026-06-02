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

        // ------------------------------
        // Register
        // ------------------------------

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

            var hash = HashPassword(password);

            var user = new User
            {
                Email = email,
                PasswordHash = hash
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }

        // ------------------------------
        // Login
        // ------------------------------

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ViewBag.ErrorKey = "ErrInvalidAuth";
                return View();
            }

            if (!VerifyPassword(password, user.PasswordHash))
            {
                ViewBag.ErrorKey = "ErrInvalidAuth";
                return View();
            }

            HttpContext.Session.SetInt32("userId", user.Id);
            HttpContext.Session.SetString("userEmail", user.Email);

            return RedirectToAction("Dashboard", "FuelLogs");
        }

        // ------------------------------
        // Logout
        // ------------------------------

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ------------------------------
        // Secure Hash Functions (PBKDF2)
        // Format: {salt}.{hash}
        // ------------------------------

        private string HashPassword(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            byte[] salt = new byte[16];
            rng.GetBytes(salt);

            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(32);

            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        private bool VerifyPassword(string password, string stored)
        {
            var parts = stored.Split('.');
            if (parts.Length != 2) return false;

            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] storedHash = Convert.FromBase64String(parts[1]);

            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            byte[] testHash = pbkdf2.GetBytes(32);

            return storedHash.SequenceEqual(testHash);
        }
    }
}