using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Noting.Models;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Noting.Services;
using MongoDB.Bson;

namespace Noting.Controllers
{
    public class AuthController : Controller
    {
        private readonly WorkoutNoteService _noteService;
        private readonly ILogger<AuthController> ?_logger;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AuthController(
            WorkoutNoteService noteService,
            ILogger<AuthController> logger)
        {
            _noteService = noteService;
            _logger = logger;
            _passwordHasher = new PasswordHasher<User>();
        }

        public IActionResult Register()
        {
            return View();
        }
        public IActionResult Login()
        {
            return View();
        }
        public IActionResult Welcome()
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

            var latest = (await _noteService.GetNotesForUserAsync())
                 .OrderByDescending(n => n.Date)
                 .FirstOrDefault();

            if (latest == null)
            {
                latest = new WorkoutNote
                {
                    UserId = existingUser.Id,
                    Date = DateTimeOffset.UtcNow,
                    NameTag = "New Note",
                    NoteText = "",
                    ExerciseIds = new List<ObjectId>()
                };
                latest = _noteService.SaveNote(latest);
            }
            return Redirect($"/note/{latest.Id}");
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

            var note = new WorkoutNote
            {
                UserId = user.Id,
                Date = DateTimeOffset.UtcNow,
                NameTag = "New Note",
                NoteText = "",
                ExerciseIds = new List<ObjectId>()
            };
            note = _noteService.SaveNote(note);

            return Redirect($"/note/{note.Id}");
        }
        private async Task SignInUserAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
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
