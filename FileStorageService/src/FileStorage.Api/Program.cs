using System.Text;
using FileStorage.Api.Auth;
using FileStorage.Api.Middleware;
using FileStorage.Application.Interfaces;
using FileStorage.Application.Options;
using FileStorage.Infrastructure;
using FileStorage.Infrastructure.Hangfire;
using FileStorage.Infrastructure.Options;
using FluentValidation;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

// ---------- Bootstrap logger (failed startup is still logged) ----------
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            path: context.Configuration["Logging:File"] ?? "/var/log/filestorage/app-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            flushToDiskInterval: TimeSpan.FromSeconds(1)));

    builder.Services.AddControllers();
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddValidatorsFromAssemblyContaining<FileStorage.Application.Validation.PrepareUploadRequestValidator>();

    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "File Storage Service", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    // ---------- Infrastructure (EF / Redis / storage / jobs) ----------
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();

    // ---------- Authentication (JWT) ----------
    var jwtKey = builder.Configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("Jwt:Key is missing.");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = builder.Configuration["Jwt:Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),
            };
        });
    builder.Services.AddAuthorization();

    // ---------- Hangfire ----------
    var hangfireConnection = builder.Configuration.GetConnectionString("FileStorage")
        ?? throw new InvalidOperationException("ConnectionStrings:FileStorage is missing.");
    builder.Services.AddHangfire(config =>
        config.UseSqlServerStorage(hangfireConnection, new Hangfire.SqlServer.SqlServerStorageOptions
        {
            PrepareSchemaIfNecessary = true,
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true,
        }));
    builder.Services.AddHangfireServer(options => options.WorkerCount = 2);

    // Schedules the nightly purge job without blocking startup (DB may be briefly down).
    builder.Services.AddHostedService<FileStorage.Infrastructure.Hangfire.RecurringJobScheduler>();

    // ---------- Server limits (Kestrel) ----------
    builder.WebHost.ConfigureKestrel(options =>
    {
        // Upper bound for a single request body; per-file limit enforced in code.
        options.Limits.MaxRequestBodySize = 1L * 1024 * 1024 * 1024; // 1 GB
    });
    builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
        o.MultipartBodyLengthLimit = 1L * 1024 * 1024 * 1024);

    var app = builder.Build();

    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto,
    });

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Swagger:Enabled", false))
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    // ---------- Hangfire dashboard (admin only) ----------
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new Hangfire.Authorization.DashboardAdminAuthorization() },
    });

    app.MapControllers();

    Log.Information("File Storage Service started.");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "File Storage Service failed to start.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public static partial class Program { }

/// <summary>Simple authorize-all filter for the Hangfire dashboard.</summary>
namespace Hangfire.Authorization
{
    using Hangfire.Dashboard;

    public sealed class DashboardAdminAuthorization : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            return httpContext.User.Identity?.IsAuthenticated == true && httpContext.User.IsInRole("Admin");
        }
    }
}