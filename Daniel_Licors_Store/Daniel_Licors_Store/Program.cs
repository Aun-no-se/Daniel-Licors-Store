using Blazored.LocalStorage;
using Daniel_Licors_Store.Components;
using Daniel_Licors_Store.Data;
using Daniel_Licors_Store.Data.Repositories.Generic;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Data.Services;
using Daniel_Licors_Store.Models.Users.Auth;
using Daniel_Licors_Store.Services;
using Daniel_Licors_Store.Services.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// =====================
// COMPONENTES
// =====================
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// =====================
// DB
// =====================
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// =====================
// REPOSITORIOS
// =====================
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// =====================
// MUD BLAZOR
// =====================
builder.Services.AddMudServices();

// =====================
// HTTP CLIENT
// =====================
builder.Services.AddHttpClient("LocalApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:7025");
});

// =====================
// SERVICES
// =====================
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<ProductStateService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AdminDashboardService>();

builder.Services.AddHttpContextAccessor();

// =====================
// LOCAL STORAGE
// =====================
builder.Services.AddBlazoredLocalStorage();

// =====================
// AUTH STATE (IMPORTANTE)
// =====================
builder.Services.AddAuthorization();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

// =====================
// COOKIE AUTH (LO DEJO POR COMPATIBILIDAD, NO ES OBLIGATORIO)
// =====================
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

var app = builder.Build();

// =====================
// ERROR HANDLING
// =====================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// =====================
// PIPELINE
// =====================
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

// =====================
// LOGIN API
// =====================
app.MapPost("/api/login", async (
    LoginModel model,
    IAuthService auth,
    HttpContext http) =>
{
    var user = await auth.ValidateLoginAsync(model);

    if (user == null)
        return Results.Unauthorized();

    var roles = user.UserRoles.Select(r => r.Role.Name).ToList();

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, user.FullName)
    };

    claims.AddRange(roles.Select(r =>
        new Claim(ClaimTypes.Role, r)));

    var identity = new ClaimsIdentity(
        claims,
        CookieAuthenticationDefaults.AuthenticationScheme
    );

    var principal = new ClaimsPrincipal(identity);

    // ⚠️ IMPORTANTE:
    // ya NO estamos usando SignInAsync porque Blazor usa LocalStorage AuthStateProvider

    return Results.Ok(new
    {
        userId = user.UserId,
        email = user.Email,
        roles
    });
});

// =====================
// RAZOR COMPONENTS
// =====================
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();