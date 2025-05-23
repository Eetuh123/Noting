using ApexCharts;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Noting.Models;
using Noting.Services;


var builder = WebApplication.CreateBuilder(args);

// � MVC for Auth + API controllers
builder.Services.AddControllersWithViews();

// � Razor Pages (needed to serve _Host.cshtml)
builder.Services.AddRazorPages();

builder.Services.AddHttpContextAccessor();

// � Mongo init
DatabaseManipulator.Initialize(builder.Configuration);

// � Cookie auth for your AuthController
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Login";
        options.Cookie.Name = "NotingCookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });
builder.Services.AddAuthorization();

// � Blazor Server
builder.Services.AddServerSideBlazor().AddCircuitOptions(opts => { opts.DetailedErrors = true; }); ;
// � Expose the auth state to Blazor components
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

// � Your service
builder.Services.AddScoped<ExerciseService>();
builder.Services.AddScoped<WorkoutNoteService>();
builder.Services.AddScoped<AppState>();
builder.Services.AddScoped<SearchState>();
builder.Services.AddScoped<ICurrentUserService, MvcCurrentUserService>();
builder.Services.AddScoped<BlazorCurrentUserService>();

builder.Services.AddApexCharts();
builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();


app.MapBlazorHub();

app.MapControllerRoute(
    name: "default",
    pattern: "/",
    defaults: new { controller = "Auth", action = "Welcome" });

app.MapControllerRoute(
    name: "auth",
    pattern: "Auth/{action=Login}/{id?}",
    defaults: new { controller = "Auth" }
);

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapRazorPages();

app.MapFallbackToPage("/_Host");

app.Run();
