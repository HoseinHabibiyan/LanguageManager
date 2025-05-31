using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace LanguageManager;

public static class LanguageManager
{
    public static void AddLanguageManager(this IServiceCollection services, Action<LanguageManagerOptions> options)
    {
        services.Configure(options);
        services.AddSingleton<ILocalization, Localization>();
        services.AddSingleton<MultiLanguageMiddleware>();
    }

    public static void UseMultiLanguage(this IApplicationBuilder app)
    {
        app.UseMiddleware<MultiLanguageMiddleware>();
    }
}

public class LanguageManagerOptions
{
    public bool ThrowExceptionIfResourceNotFound { get; set; }
    public required string ResourcesPath { get; set; }
    public required IEnumerable<string> Cultures { get; set; }
    public required TimeSpan CacheExpiration { get; set; }
}

public interface ILocalization
{
    Task<Dictionary<string, string>> Get(CancellationToken ct);
    Task<string> Get(string key, CancellationToken ct);
    Task<Dictionary<string, string>> Get(IEnumerable<string> keys, CancellationToken ct);
}

public class Localization(HybridCache cache, IOptionsMonitor<LanguageManagerOptions> options) : ILocalization
{
    private readonly HybridCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    public async Task<Dictionary<string, string>> Get(CancellationToken ct)
    {
        return await GetAllString(ct);
    }

    public async Task<string> Get(string key, CancellationToken ct)
    {
        var result = await GetAllString(ct);
        return result.TryGetValue(key, out var value) ? value : string.Empty;
    }

    public async Task<Dictionary<string, string>> Get(IEnumerable<string> keys, CancellationToken ct)
    {
        var result = await GetAllString(ct);
        return result.Where(r => keys.Any(k => k.Equals(r.Key))).ToDictionary(r => r.Key, r => r.Value);
    }

    private async Task<Dictionary<string, string>> GetAllString(CancellationToken ct)
    {
        var culture = Thread.CurrentThread.CurrentCulture;

        string cacheKey = $"{culture}-LANGUAGE-CACHE-KEY";

        return await _cache.GetOrCreateAsync(cacheKey, async c => await GetResource(culture.ToString(), c),
            new HybridCacheEntryOptions()
            {
                Expiration = options.CurrentValue.CacheExpiration
            }, cancellationToken: ct);
    }

    private async Task<Dictionary<string, string>> GetResource(string culture, CancellationToken ct)
    {
        string path = Path.Combine(options.CurrentValue.ResourcesPath, $"{culture}.json");

        if (!File.Exists(path))
        {
            if (options.CurrentValue.ThrowExceptionIfResourceNotFound)
                throw new FileNotFoundException("Resource not found", path);

            return new Dictionary<string, string>();
        }

        var json = await File.ReadAllTextAsync(path, ct);

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;
    }
}

public class MultiLanguageMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var cultureKey = context.Request.Headers["Accept-Language"];
        if (!string.IsNullOrEmpty(cultureKey) && DoesCultureExist(cultureKey!))
        {
            var culture = new CultureInfo(cultureKey!);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }

        await next(context);
    }

    private static bool DoesCultureExist(string cultureName)
    {
        return CultureInfo.GetCultures(CultureTypes.AllCultures)
            .Any(culture => string.Equals(culture.Name, cultureName,
                StringComparison.CurrentCultureIgnoreCase));
    }
}