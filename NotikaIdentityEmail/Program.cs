using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NotikaIdentityEmail.Context;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Hubs;
using NotikaIdentityEmail.Logging;
using NotikaIdentityEmail.Models;
using NotikaIdentityEmail.Models.IdentityModels;
using NotikaIdentityEmail.Services;
using NotikaIdentityEmail.Services.CategoryServices;
using NotikaIdentityEmail.Services.CommentServices;
using NotikaIdentityEmail.Services.DashboardServices;
using NotikaIdentityEmail.Services.EmailServices;
using NotikaIdentityEmail.Services.HuggingFaces;
using NotikaIdentityEmail.Services.LoginServices;
using NotikaIdentityEmail.Services.MessageServices;
using NotikaIdentityEmail.Services.RegisterServices;
using NotikaIdentityEmail.Services.RoleServices;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------
// LOGGING
// --------------------------------------------------
builder.Logging.ClearProviders();
builder.Services.AddSingleton<ILoggerProvider, ElasticLoggerProvider>();

// --------------------------------------------------
// DB
// --------------------------------------------------
builder.Services.AddDbContext<EmailContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=localhost\\SQLEXPRESS;Database=NotikaEmailDb;Integrated Security=true;TrustServerCertificate=true"
    ));

// --------------------------------------------------
// IDENTITY
// --------------------------------------------------
builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<EmailContext>()
    .AddErrorDescriber<CustomIdentityValidator>()
    .AddTokenProvider<DataProtectorTokenProvider<AppUser>>(TokenOptions.DefaultProvider);

// 🔴 KRİTİK: Identity email doğrulama kilidini KALDIRIYORUZ
builder.Services.Configure<IdentityOptions>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;

    options.User.RequireUniqueEmail = true;
});

// --------------------------------------------------
// CONFIG
// --------------------------------------------------
builder.Services.Configure<JwtSettingsModel>(
    builder.Configuration.GetSection("JwtSettingsKey"));

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<HuggingFaceOptions>(
    builder.Configuration.GetSection("HuggingFace"));
// --------------------------------------------------
// SERVICES
// --------------------------------------------------
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IRegisterService, RegisterService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddSingleton<IHtmlSanitizerService, HtmlSanitizerService>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IHuggingFaceService, HuggingFaceService>();
builder.Services.AddHttpClient<ElasticLogService>();
builder.Services.AddHostedService<ElasticIndexSetupService>();

// --------------------------------------------------
// AUTH
// --------------------------------------------------
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Login/UserLogin";
    options.AccessDeniedPath = "/Error/403";
})
.AddJwtBearer(opt =>
{
    var jwtSettings = builder.Configuration
        .GetSection("JwtSettingsKey")
        .Get<JwtSettingsModel>();

    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings!.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.Key))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// --------------------------------------------------
// MVC + SIGNALR
// --------------------------------------------------
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

var app = builder.Build();

// --------------------------------------------------
// MIDDLEWARE
// --------------------------------------------------
app.Use(async (context, next) =>
{
    var userEmail = context.User?.Identity?.Name
        ?? context.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

    if (!string.IsNullOrWhiteSpace(userEmail))
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        using (logger.BeginScope(new Dictionary<string, object?> { ["UserEmail"] = userEmail }))
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

// --------------------------------------------------
// START
// --------------------------------------------------
try
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    using (logger.BeginScope(new Dictionary<string, object?> { ["OperationType"] = LogContextValues.OperationSystem }))
    {
        logger.LogInformation(SystemLogMessages.SystemStarted);
    }
   

    app.Run();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    using (logger.BeginScope(new Dictionary<string, object?> { ["OperationType"] = LogContextValues.OperationSystem }))
    {
        logger.LogCritical(ex, LogMessages.UnexpectedError);
    }
}
finally
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    using (logger.BeginScope(new Dictionary<string, object?> { ["OperationType"] = LogContextValues.OperationSystem }))
    {
        logger.LogInformation(SystemLogMessages.SystemStopped);
    }
}
