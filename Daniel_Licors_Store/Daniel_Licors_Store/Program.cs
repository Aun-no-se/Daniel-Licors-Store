using Blazored.LocalStorage;
using Daniel_Licors_Store.Components;
using Daniel_Licors_Store.Data;
using Daniel_Licors_Store.Data.Repositories.Generic;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Data.Services;
using Daniel_Licors_Store.Data.Services.Interfaces;
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

#region ===================== LOGGING (CRÍTICO PARA DEBUG) =====================
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Trace);
#endregion

#region ===================== COMPONENTES =====================
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 32 * 1024 * 1024; // uploads grandes
    });
#endregion

#region ===================== DB =====================
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));
#endregion

#region ===================== REPOSITORIOS =====================
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
#endregion

#region ===================== MUD BLAZOR =====================
builder.Services.AddMudServices();
#endregion

#region ===================== HTTP CLIENT =====================
builder.Services.AddHttpClient("LocalApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:7025");
});
#endregion

#region ===================== SERVICES =====================
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<ProductStateService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AdminDashboardService>();
builder.Services.AddScoped<IComboService, ComboService>();
#endregion

builder.Services.AddHttpContextAccessor();

#region ===================== LOCAL STORAGE =====================
builder.Services.AddBlazoredLocalStorage();
#endregion

#region ===================== AUTH =====================
builder.Services.AddAuthorization();

builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

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
#endregion

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 64 * 1024 * 1024; // 64MB
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
        options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    });

builder.Services.AddServerSideBlazor(options =>
{
    options.DetailedErrors = builder.Environment.IsDevelopment();
});

var app = builder.Build();

#region ===================== GLOBAL ERROR HANDLER (CLAVE PARA -1) =====================
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine("ERROR GLOBAL CAPTURADO:");
        Console.WriteLine(ex.ToString());
        throw;
    }
});
#endregion

#region ===================== ERROR PAGES =====================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
#endregion

#region ===================== PIPELINE =====================
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();
#endregion

#region ===================== LOGIN API =====================
app.MapPost("/api/login", async (
    LoginModel model,
    IAuthService auth) =>
{
    try
    {
        var user = await auth.ValidateLoginAsync(model);

        if (user == null)
            return Results.Unauthorized();

        var roles = user.UserRoles.Select(r => r.Role.Name).ToList();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(ClaimTypes.Name, user.FullName ?? "")
        };

        claims.AddRange(roles.Select(r =>
            new Claim(ClaimTypes.Role, r)));

        return Results.Ok(new
        {
            userId = user.UserId,
            email = user.Email,
            roles
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine(" ERROR LOGIN:");
        Console.WriteLine(ex.ToString());
        return Results.Problem("Error interno en login");
    }
});
#endregion

#region ===================== RAZOR COMPONENTS =====================
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
#endregion

AppDomain.CurrentDomain.UnhandledException += (s, e) =>
{
    File.WriteAllText("fatal.txt", e.ExceptionObject.ToString());
};


app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var ex = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (ex?.Error != null)
            Console.WriteLine($" CIRCUIT ERROR: {ex.Error}");
    });
});
app.Run();