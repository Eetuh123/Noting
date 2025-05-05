using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Noting.Models;
using Noting.Services;

var builder = WebApplication.CreateBuilder(args);

// — MVC for Auth + API controllers
builder.Services.AddControllersWithViews();

// — Razor Pages (needed to serve _Host.cshtml)
builder.Services.AddRazorPages();



// — Mongo init
DatabaseManipulator.Initialize(builder.Configuration);

// — Cookie auth for your AuthController
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Login";
        options.Cookie.Name = "NotingCookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });
builder.Services.AddAuthorization();

// — Blazor Server
builder.Services.AddServerSideBlazor();
// — Expose the auth state to Blazor components
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

// — Your service
builder.Services.AddScoped<ExerciseService>();
builder.Services.AddScoped<WorkoutNoteService>();

var app = builder.Build();


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// 1) Blazor’s real-time SignalR endpoint
app.MapBlazorHub();

// 2) MVC controllers under specific prefixes
app.MapControllerRoute(
    name: "auth",
    pattern: "Auth/{action=Login}/{id?}",
    defaults: new { controller = "Auth" }
);

// 3) Razor Pages (so _Host.cshtml can be served)
app.MapRazorPages();

// 4) Fallback *all* other URLs (like /hello) to the Blazor host page
app.MapFallbackToPage("/_Host");

app.Run();
