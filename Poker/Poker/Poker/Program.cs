using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Poker.Components;
using Poker.Game;
using Poker.Models;
using Poker.Services;
using PokerServer.Hubs;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });

builder.Services.AddOpenApi();

builder.Services.AddDbContext<PokerContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.Name = "accessToken";
    options.LoginPath = "/";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
})
.AddJwtBearer(options =>
{
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["accessToken"];
            return Task.CompletedTask;
        }
    };
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["AppSettings:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["AppSettings:Audience"],
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["AppSettings:Token"]!)),
        ValidateIssuerSigningKey = true
    };
});

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<UtilService>();

builder.Services.AddScoped(sp =>
{
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    return new HttpClient
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };
});

builder.Services.AddSingleton<GameService>();
builder.Services.AddSingleton<IHandEvaluator>(sp =>
    new LutEvaluator("/home/pi/poker-data/nonflush_lut.dat", "/home/pi/poker-data/flush_lut.dat"));
builder.Services.AddSignalR();
builder.Services.AddHostedService<TokenCleanupService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.UseCors(policy =>
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader());

app.MapHub<PokerHub>("/pokerhub");

app.MapGet("/api/assets/manifest", (IWebHostEnvironment env) =>
{
    var root = env.WebRootPath;

    if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
    {
        return Results.Problem("Serwer nie mo¿e zlokalizowaæ folderu wwwroot.");
    }

    var foldersToScan = new[] { "Images", "Sounds" };

    var files = foldersToScan
        .SelectMany(folder => {
            var fullPath = Path.Combine(root, folder);

            if (!Directory.Exists(fullPath))
            {
                var capitalizedFolder = char.ToUpper(folder[0]) + folder.Substring(1);
                var altPath = Path.Combine(root, capitalizedFolder);
                if (Directory.Exists(altPath)) fullPath = altPath;
            }

            return Directory.Exists(fullPath)
                ? Directory.GetFiles(fullPath, "*.*", SearchOption.AllDirectories)
                : Enumerable.Empty<string>();
        })
        .Select(file => Path.GetRelativePath(root, file).Replace("\\", "/"))
        .ToList();

    return Results.Ok(files);
});
app.Run();
