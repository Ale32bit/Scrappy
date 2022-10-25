using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Scrappy.Models;

namespace Scrappy.PluginLoader;
public class JsonPlugin : IPlugin
{
    public const string ConfigurationPath = "Configuration/Connections.json";

    private readonly ILogger<JsonPlugin> _logger;

    public JsonPlugin(ILogger<JsonPlugin> logger)
    {
        _logger = logger;

    }

    public async Task Init()
    {
        _logger.LogInformation("Loaded JSON plugin");
    }

    public async Task<IEnumerable<RemoteHost>> GetRemoteHostsAsync()
    {
        if (!File.Exists(ConfigurationPath))
            return Enumerable.Empty<RemoteHost>();

        var content = await File.ReadAllTextAsync(ConfigurationPath);
        var remoteHosts = JsonConvert.DeserializeObject<IEnumerable<RemoteHost>>(content);

        if (remoteHosts == null)
            return Enumerable.Empty<RemoteHost>();

        return remoteHosts;
    }
}
