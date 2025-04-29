using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Noting.Services;

namespace Noting.Controllers
{
    public class ExerciseController : Controller
    {
        private readonly ExerciseService _exerciseService;

        public ExerciseController(ExerciseService exerciseService)
        {
            _exerciseService = exerciseService;
        }
        public IActionResult Login()
        {
            return View();
        }
    }
}
