using Blazored.LocalStorage;
using Daniel_Licors_Store.Components;
using Daniel_Licors_Store.Data;
using Daniel_Licors_Store.Data.Repositories.Generic;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Data.Services;
using Daniel_Licors_Store.Data.Services.Interfaces;
using Daniel_Licors_Store.Services;
using Daniel_Licors_Store.Services.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// ── LOGGING ───────────────────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(
    builder.Environment.IsDevelopment() ? LogLevel.Warning : LogLevel.Warning);

// ── RAZOR COMPONENTS (UNA SOLA VEZ) ──────────────────────────────────────────
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 64 * 1024 * 1024;
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
        options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    });

builder.Services.AddServerSideBlazor(options =>
{
    options.DetailedErrors = builder.Environment.IsDevelopment();
});

// ── BASE DE DATOS ─────────────────────────────────────────────────────────────
// DbContextFactory para Repository<T> (usa IDbContextFactory internamente)
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// DbContext scoped para servicios que lo reciben por constructor
// (CatalogService, ProductService, AdminDashboardService, etc.)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ── MUD BLAZOR ────────────────────────────────────────────────────────────────
builder.Services.AddMudServices();

// ── LOCAL STORAGE ─────────────────────────────────────────────────────────────
builder.Services.AddBlazoredLocalStorage();

// ── AUTORIZACIÓN ──────────────────────────────────────────────────────────────
// Solo AddAuthorization — sin cookie auth (la auth la maneja LocalStorage + HMAC)
builder.Services.AddAuthorization();
// SessionSigner: singleton porque la clave HMAC es fija y es seguro compartirla
builder.Services.AddSingleton<SessionSigner>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

// ── HTTP CONTEXT ──────────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();

// ── REPOSITORIOS ──────────────────────────────────────────────────────────────
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// ── SERVICIOS ─────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IComboService, ComboService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
builder.Services.AddScoped<ProductStateService>();
builder.Services.AddScoped<AdminDashboardService>();
builder.Services.AddScoped<StockGeneralService>();

// ── APP ───────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

AppDomain.CurrentDomain.UnhandledException += (_, e) =>
    File.WriteAllText("fatal.txt", e.ExceptionObject.ToString());

app.Run();
