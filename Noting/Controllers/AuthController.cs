using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Noting.Models;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Noting.Controllers
{
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> ?_logger;
        private readonly PasswordHasher<User> _passwordHasher = new();
        public IActionResult Register()
        {
            return View();
        }
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginUser(User user)
        {
            var email = user.Email;
            var password = user.PasswordHash;

            var existingUser = DatabaseManipulator.database?
                .GetCollection<User>("User")
                .Find(u => u.Email == email)
                .FirstOrDefault();
            if (existingUser == null)
                return RedirectToAction("Login", "Auth");

            bool isPlainText = !existingUser.PasswordHash.StartsWith("$2") && !existingUser.PasswordHash.StartsWith("AQAAAA");
            if (isPlainText && existingUser.PasswordHash == password)
            {
                existingUser.PasswordHash = _passwordHasher.HashPassword(existingUser, password);
                DatabaseManipulator.database?
                    .GetCollection<User>("User")
                    .ReplaceOne(u => u.Id == existingUser.Id, existingUser);
            }

            else if (_passwordHasher.VerifyHashedPassword(existingUser, existingUser.PasswordHash, password) != PasswordVerificationResult.Success)
            {
                return RedirectToAction("Login", "Auth");
            }

            await SignInUserAsync(existingUser);

            return RedirectToAction("Index", "Main");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterUser(User user)
        {
            var collection = DatabaseManipulator.database?
                .GetCollection<User>("User");

            if (collection.Find(u => u.Email == user.Email).FirstOrDefault() != null)
            { 
                ModelState.AddModelError("Email", "Email already exists");
                return View("Login");
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, user.PasswordHash);
            DatabaseManipulator.Save(user);

            await SignInUserAsync(user);

            return RedirectToAction("Index", "Main");
        }
        private async Task SignInUserAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            };
            var claimsIdentity = new ClaimsIdentity(claims, "User");
            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
                IsPersistent = true,
            };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity), authProperties);
        }
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }
    }
}
