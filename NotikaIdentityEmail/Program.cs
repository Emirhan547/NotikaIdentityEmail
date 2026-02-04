using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NotikaIdentityEmail.Context;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Hubs;
using NotikaIdentityEmail.Models;
using NotikaIdentityEmail.Models.IdentityModels;
using NotikaIdentityEmail.Services;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ✅ Serilog bootstrapping
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// ✅ DÜZELTME: DbContext artık connection string kullanıyor
builder.Services.AddDbContext<EmailContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=localhost\\SQLEXPRESS;initial Catalog=NotikaEmailDb;integrated security=true;trustServerCertificate=true"
    ));

builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<EmailContext>()
    .AddErrorDescriber<CustomIdentityValidator>()
    .AddTokenProvider<DataProtectorTokenProvider<AppUser>>(TokenOptions.DefaultProvider);

builder.Services.Configure<JwtSettingsModel>(builder.Configuration.GetSection("JwtSettingsKey"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddScoped<IEmailService, EmailService>();

// ✅ DÜZELTME: HttpClient factory kullanımı
builder.Services.AddHttpClient<ElasticLogService>();

// ✅ YENİ: Elasticsearch index template setup
builder.Services.AddHostedService<ElasticIndexSetupService>();

builder.Services.AddSingleton<IHtmlSanitizerService, HtmlSanitizerService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Login/UserLogin";
    options.AccessDeniedPath = "/Error/403";
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opt =>
{
    var jwtSettings = builder.Configuration.GetSection("JwtSettingsKey").Get<JwtSettingsModel>();
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings!.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddSignalR();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ✅ Global request logging (HTTP)
app.UseSerilogRequestLogging(opts =>
{
    opts.GetLevel = (httpCtx, elapsedMs, ex) =>
    {
        if (ex != null) return LogEventLevel.Error;
        if (httpCtx.Response.StatusCode >= 500) return LogEventLevel.Error;
        if (httpCtx.Response.StatusCode >= 400) return LogEventLevel.Warning;
        return LogEventLevel.Information;
    };
});

// ✅ YENİ: User email enrichment middleware
app.Use(async (context, next) =>
{
    var userEmail = context.User?.Identity?.Name ?? context.User?.Claims
        .FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

    if (!string.IsNullOrEmpty(userEmail))
    {
        using (LogContext.PushProperty("UserEmail", userEmail))
        {
            await next();
        }
    }
    else
    {
        await next();
    }
});

app.UseStatusCodePagesWithReExecute("/Error/{0}");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
 name: "areas",
 pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<NotificationHub>("/hubs/notification");

try
{
    Log.Information("Application starting up");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}