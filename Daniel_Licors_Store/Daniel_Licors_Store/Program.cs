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
    builder.Environment.IsDevelopment() ? LogLevel.Debug : LogLevel.Warning);

// ── RAZOR COMPONENTS ──────────────────────────────────────────
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 32 * 1024 * 1024;
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
        options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    });

// ── SERVER SIDE BLAZOR ────────────────────────────────────────────────────────
builder.Services.AddServerSideBlazor(options =>
{
    options.DetailedErrors = builder.Environment.IsDevelopment();
});

// ── BASE DE DATOS ─────────────────────────────────────────────────────────────
// DbContextFactory para evitar el error de concurrencia en Blazor Server
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// DbContext con scope normal para servicios que lo necesiten directamente
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// ── MUD BLAZOR ────────────────────────────────────────────────────────────────
builder.Services.AddMudServices();

// ── LOCAL STORAGE ─────────────────────────────────────────────────────────────
builder.Services.AddBlazoredLocalStorage();

// ── AUTORIZACIÓN ──────────────────────────────────────────────────────────────
// Solo AddAuthorization — sin AddAuthentication Cookie porque la auth
// la maneja CustomAuthStateProvider con LocalStorage, no cookies de ASP.NET
builder.Services.AddAuthorization();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

// ── HTTP CONTEXT ──────────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();

// ── REPOSITORIOS ──────────────────────────────────────────────────────────────
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// ── SERVICIOS DE NEGOCIO ──────────────────────────────────────────────────────
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

// ── BUILD APP ─────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── MANEJO DE ERRORES ─────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// ── PIPELINE HTTP ─────────────────────────────────────────────────────────────
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthorization();

// ── RAZOR COMPONENTS ──────────────────────────────────────────────────────────
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ── UNHANDLED EXCEPTIONS ──────────────────────────────────────────────────────
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    File.WriteAllText("fatal.txt", e.ExceptionObject.ToString());
};

app.Run();
