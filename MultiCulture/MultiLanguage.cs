using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace MultiCulture;

public static class MultiLanguage
{
    public static void AddMultiLanguage(this IServiceCollection services , Action<MultiLanguageOptions> options)
    {
        services.Configure<MultiLanguageOptions>(options);
         services.AddSingleton<ILocalization,Localization>();
    }
}

public class MultiLanguageOptions()
{
    public required string ResourcesPath { get; set; }
    public required IEnumerable<string> Cultures{ get; set; }
}

public class Localization(IMemoryCache memoryCache ,IOptionsMonitor<MultiLanguageOptions> options) : ILocalization
{
    private readonly IMemoryCache _memoryCache = memoryCache;

    public Dictionary<string,string> GetAllString()
    {
        foreach (var culture in options.CurrentValue.Cultures)
        {
            string path = Path.Combine(options.CurrentValue.ResourcesPath, $"{culture}.json");

            if (!File.Exists(path))
                throw new Exception("culture not exist");
            
            string? langArr = File.ReadAllText(path);

            if (langArr is null)
                throw new Exception($"{culture} is null");
            
            Dictionary<string, string> dic = JsonSerializer.Deserialize<Dictionary<string, string>>(langArr)!;
            return dic;
        }

        throw new Exception();
    }
}

public interface ILocalization
{ 
    Dictionary<string,string> GetAllString();
}

public record MultiLanguageDic(string Lang,string Value);