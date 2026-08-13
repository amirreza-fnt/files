namespace FileStorage.Infrastructure;

using FileStorage.Application.Cache;
using FileStorage.Application.Interfaces;
using FileStorage.Application.Options;
using FileStorage.Application.Services;
using FileStorage.Infrastructure.Hangfire;
using FileStorage.Infrastructure.Options;
using FileStorage.Infrastructure.Persistence;
using FileStorage.Infrastructure.Redis;
using FileStorage.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ---------- Options ----------
        services.Configure<UploadOptions>(configuration.GetSection(UploadOptions.SectionName));
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));
        services.Configure<DeleteOptions>(configuration.GetSection(DeleteOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<InternalAuthOptions>(configuration.GetSection(InternalAuthOptions.SectionName));

        // Cache store (Redis-compatible). The server uses Valkey (a drop-in Redis fork),
        // so we prefer the "Valkey" section and fall back to "Redis" when absent.
        services.Configure<RedisOptions>(options =>
        {
            var valkey = configuration.GetSection("Valkey");
            if (valkey.Exists() && !string.IsNullOrWhiteSpace(valkey["ConnectionString"]))
            {
                valkey.Bind(options);
            }
            else
            {
                configuration.GetSection(RedisOptions.SectionName).Bind(options);
            }
        });

        // ---------- MSSQL (remote Windows server) ----------
        var connectionString = configuration.GetConnectionString("FileStorage")
            ?? throw new InvalidOperationException("ConnectionStrings:FileStorage is missing.");

        services.AddDbContext<FileStorageDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                // MSSQL lives on a separate Windows host over the network:
                // retry transient connection failures instead of failing fast.
                sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                sql.CommandTimeout(30);
            }));

        // ---------- Redis ----------
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var redisOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisOptions>>().Value;
            var config = ConfigurationOptions.Parse(redisOptions.ConnectionString);
            config.AbortOnConnectFail = false;   // degrade gracefully when Redis is briefly down
            config.ConnectRetry = 5;
            config.ConnectTimeout = 5000;
            config.SyncTimeout = 5000;
            return ConnectionMultiplexer.Connect(config);
        });

        // ---------- Repositories ----------
        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<IStaticFileRepository, StaticFileRepository>();
        services.AddScoped<IFileGroupRepository, FileGroupRepository>();

        // ---------- Cache / Redis-backed stores ----------
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IFileMetadataCache, FileMetadataCache>();
        services.AddScoped<IFileCacheInvalidator, FileCacheInvalidator>();
        services.AddScoped<ITempLinkStore, TempLinkStore>();
        services.AddScoped<IUploadTokenStore, UploadTokenStore>();

        // ---------- Storage ----------
        services.AddScoped<IFileStorage, DiskFileStorage>();

        // ---------- Application services ----------
        services.AddScoped<IFileAuthorizationService, FileAuthorizationService>();
        services.AddScoped<IFileUploadService, FileUploadService>();
        services.AddScoped<IFileReadService, FileReadService>();
        services.AddScoped<IFileDeleteService, FileDeleteService>();

        services.AddScoped<PurgeDeletedFilesJob>();

        return services;
    }
}