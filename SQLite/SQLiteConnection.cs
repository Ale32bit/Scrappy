using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scrappy.PluginLoader;
using Scrappy.Models;
using SQLite.Data;

namespace SQLite;
class SQLiteConnection : IPlugin
{
    public static void Main() { }

    private readonly ILogger<SQLiteConnection> _logger;
    private readonly IConfiguration _configuration;

    private DataContext Context;
    public SQLiteConnection(ILogger<SQLiteConnection> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public Task Init()
    {
        _logger.LogInformation("SQLite connection ready");
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<RemoteHost>> GetRemoteHostsAsync()
    {
        var remoteHosts = new List<RemoteHost>();

        Context = new DataContext(_configuration.GetConnectionString("SQLite"));
        if (await Context.Database.EnsureCreatedAsync())
        {
            _logger.LogInformation("Database created!");
        }

        foreach (var host in Context.Hosts)
        {
            var remoteHost = new RemoteHost
            {
                Name = host.Name,
                Address = host.Address,
                Legacy = host.Legacy,
                User = new()
                {
                    Domain = host.User.Domain,
                    Username = host.User.Username,
                    Password = host.User.Password,
                },
            };

            var remoteShares = new List<RemoteShare>();

            foreach (var share in host.Shares)
            {
                var remoteShare = new RemoteShare
                {
                    Name = share.Name,
                    AppendMode = share.AppendMode,
                    Ignore = share.Ignore,
                };

                remoteShares.Add(remoteShare);
            }

            remoteHost.Shares = remoteShares;
            remoteHosts.Add(remoteHost);
        }

        return remoteHosts;
    }

    public void Dispose()
    {
        Context.Dispose();
    }
}
