using Newtonsoft.Json;
using Scrappy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scrappy.Connections;
public class JsonConnection : IConnection
{
    public const string ConfigurationPath = "Configuration/Connections.json";

    private readonly ILogger<JsonConnection> _logger;

    public JsonConnection(ILogger<JsonConnection> logger)
    {
        _logger = logger;
    }

    public async Task Init()
    {
        _logger.LogInformation("Loaded JSON Connections plugin");
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

    public void Dispose()
    {

    }
}
