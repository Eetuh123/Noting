using Noting;

var builder = WebApplication.CreateBuilder(args);

// For MVC
builder.Services.AddControllersWithViews();

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// Blazor Server
builder.Services.AddServerSideBlazor();

// SignalR Blazor Server needs it for something ???
builder.Services.AddSignalR();


var app = builder.Build();

app.MapBlazorHub();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.UseAntiforgery();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
