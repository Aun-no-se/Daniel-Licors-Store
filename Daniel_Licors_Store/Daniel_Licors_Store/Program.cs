using Blazored.LocalStorage;
using Daniel_Licors_Store.Components;
using Daniel_Licors_Store.Data;
using Daniel_Licors_Store.Data.Repositories.Generic;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Data.Services;
using Daniel_Licors_Store.Data.Services.Interfaces;
using Daniel_Licors_Store.Services;
using Daniel_Licors_Store.Services.Auth;
using Microsoft.AspNetCore.Authentication;
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
        // Antes 64 MB — muy por encima de lo que la app realmente necesita
        // (las imágenes se topan en 5 MB en ImageUploadValidator; en base64
        // eso son ~7 MB más el resto del mensaje). Un límite más ajustado
        // reduce la superficie de un DoS por payloads gigantes contra el
        // circuito de Blazor Server, sin afectar el uso normal de la app.
        options.MaximumReceiveMessageSize = 10 * 1024 * 1024;
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
        options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    });

builder.Services.AddServerSideBlazor(options =>
{
    options.DetailedErrors = builder.Environment.IsDevelopment();
});

// ── BASE DE DATOS ─────────────────────────────────────────────────────────────
// Una sola fuente de verdad para la cadena de conexión: el factory.
// IDbContextFactory<ApplicationDbContext> lo usan los servicios que crean su
// propio contexto por operación (evita el TaskCanceledException de Blazor
// Server cuando el pre-render cancela el scope antes de que termine una query).
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ApplicationDbContext scoped, para los servicios que lo reciben directo por
// constructor — se crea A PARTIR del mismo factory de arriba en vez de tener
// su propia configuración de UseSqlServer duplicada. Antes había dos
// registros separados apuntando a la misma cadena de conexión: funcionaban,
// pero si alguien cambiaba la config en uno y se le olvidaba el otro,
// quedaban desincronizados sin ningún aviso.
builder.Services.AddScoped<ApplicationDbContext>(sp =>
    sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

// ── MUD BLAZOR ────────────────────────────────────────────────────────────────
builder.Services.AddMudServices();

// ── LOCAL STORAGE ─────────────────────────────────────────────────────────────
builder.Services.AddBlazoredLocalStorage();

// ── AUTORIZACIÓN ──────────────────────────────────────────────────────────────
// AddAuthentication con esquema dummy — la auth real la maneja CustomAuthStateProvider
// con LocalStorage + HMAC. El esquema es necesario para que UseAuthorization()
// pueda llamar a ChallengeAsync sin explotar cuando el usuario no tiene acceso.
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "NoOpScheme";
    options.DefaultChallengeScheme = "NoOpScheme";
})
.AddScheme<AuthenticationSchemeOptions, NoOpAuthenticationHandler>("NoOpScheme", _ => { });

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
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IComboService, ComboService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
builder.Services.AddScoped<ProductStateService>();
builder.Services.AddScoped<AdminDashboardService>();
builder.Services.AddScoped<StockGeneralService>();
builder.Services.AddScoped<UserManagementService>();
builder.Services.AddScoped<CategoryBrandService>();
builder.Services.AddScoped<HiddenItemsService>();

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

// ── PÁGINA 404 TEMÁTICA ──────────────────────────────────────────────────────
// Blazor's <NotFound> del Router SOLO funciona para navegación interna
// (cuando ya estás dentro de la app y das click a un link que no existe).
// Si alguien escribe una URL a mano, refresca, o entra por un link externo
// a una ruta que no existe, ASP.NET Core devuelve un 404 crudo ANTES de que
// Blazor cargue — por eso Edge/Chrome mostraban su propia pantalla de error
// en vez de nuestra página. Esto reejecuta el pipeline contra /not-found
// (que sí es una @page real) manteniendo el status code 404.
app.UseStatusCodePagesWithReExecute("/not-found");

app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Antes esto escribía un fatal.txt en texto plano en la carpeta de la app —
// sin pasar por ningún nivel de log, sin rotación, y con el detalle completo
// de la excepción (que puede incluir datos sensibles) quedando ahí tal cual.
// Usamos el logger real de la app, que ya está configurado arriba
// (Console + Debug), para que las excepciones no controladas queden
// integradas en el mismo lugar que el resto de los logs.
var fatalLogger = app.Services.GetRequiredService<ILogger<Program>>();
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
    fatalLogger.LogCritical(
        e.ExceptionObject as Exception,
        "Excepción no controlada (UnhandledException). IsTerminating={IsTerminating}",
        e.IsTerminating);

app.Run();
