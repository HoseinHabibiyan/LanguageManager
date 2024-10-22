using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LanguageManager;

public static class LanguageManager
{
    public static void AddLanguageManager(this IServiceCollection services , Action<LanguageManagerOptions> options)
    {
        services.Configure(options);
         services.AddSingleton<ILocalization,Localization>();
         services.AddSingleton<MultiLanguageMiddleware>();
    }
    
    public static IApplicationBuilder  UseMultiLanguage(this IApplicationBuilder  app)
    {
        return  app.UseMiddleware<MultiLanguageMiddleware>();
    }
}

public class LanguageManagerOptions()
{
    public bool ThrowExceptionIfResourceNotFound { get; set; }
    public required string ResourcesPath { get; set; }
    public required IEnumerable<string> Cultures{ get; set; }
}

public interface ILocalization
{
    Dictionary<string, string> Get();
    string Get(string key);
    Dictionary<string, string> Get(IEnumerable<string> keys);
}

public class Localization(IMemoryCache memoryCache ,IOptionsMonitor<LanguageManagerOptions> options) : ILocalization
{
    public Dictionary<string, string> Get()
    {
        return GetAllString();
    }
    
    public string Get(string key)
    {
        var result = GetAllString();
        return result.TryGetValue(key, out var value) ? value : string.Empty;
    }
    
    public Dictionary<string, string> Get(IEnumerable<string> keys)
    {
        var result = GetAllString();
        return result.Where(r=>keys.Any(k=>k.Equals(r.Key))).ToDictionary(r=>r.Key, r=>r.Value);
    }
    
    private Dictionary<string,string> GetAllString()
    {
        var culture = Thread.CurrentThread.CurrentCulture;

        string cacheKey = $"{culture}-LANGUAGE-CACHE-KEY";

        return memoryCache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetOptions(new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10)));
          return  GetResource(culture.ToString());
        })!;
    }

    private Dictionary<string, string> GetResource(string culture)
    {
        string path = Path.Combine(options.CurrentValue.ResourcesPath, $"{culture}.json");

        if (!File.Exists(path))
        { 
            if(options.CurrentValue.ThrowExceptionIfResourceNotFound)
                throw new FileNotFoundException("Resource not found", path);
            
            return new Dictionary<string, string>();
        }

        string json = File.ReadAllText(path);

        Dictionary<string, string> dic = JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;
        
        return dic;
    }
}

public class MultiLanguageMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var cultureKey = context.Request.Headers["Accept-Language"];
        if (!string.IsNullOrEmpty(cultureKey))
        {
            if (DoesCultureExist(cultureKey))
            {
                var culture = new CultureInfo(cultureKey);
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
            }
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