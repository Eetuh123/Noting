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
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> LoginUser(string email, string password)
        //{
        //    var user = DatabaseManipulator.database?
        //        .GetCollection<User>("User")
        //        .Find(u => u.Email == email)
        //        .FirstOrDefault();
        //}
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

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, user.Email)
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

            return RedirectToAction("Index", "Main");
        }
    }
}
