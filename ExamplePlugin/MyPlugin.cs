using Microsoft.Extensions.Logging;
using Scrappy.Events;
using Scrappy.Models;
using Scrappy.PluginLoader;

namespace ExamplePlugin;

public class MyPlugin : IPlugin
{
    private readonly ILogger _logger;

    public MyPlugin(ILogger<MyPlugin> logger)
    {
        _logger = logger;
    }

    public async Task PreInit()
    {
        _logger.LogInformation("Pre init MyPlugin");
    }
    public async Task Init()
    {
        _logger.LogInformation("Init MyPlugin! Hello, World!");
    }
    public async Task<IEnumerable<RemoteHost>> GetRemoteHostsAsync()
    {
        return Enumerable.Empty<RemoteHost>();
    }

    public void OnScrapeCycleStart() {
        _logger.LogInformation("A scrape cycle is starting!");
    }
    public void OnScrapeCycleEnd() {
        _logger.LogInformation("A scrape cycle has ended!");
    }
    public void OnScrapeCycleTimeout() {
        _logger.LogInformation("A scrape cycle has timed out!");
    }
    public void OnFileDownload(FileDownloadEvent ev) {
        _logger.LogInformation("Downloading '{source}' from {hostName} ({hostAddress})", ev.SourceFilePath, ev.RemoteHost.Name, ev.RemoteHost.Address);
    }
    public void OnFileWrite(FileWriteEvent ev) {
        _logger.LogInformation("Saved file '{destination}', Append Mode: {append}", ev.DestinationFilePath, ev.AppendMode);
    }
    public void OnScrapeHostStart(ScrapeHostEvent ev) {
        _logger.LogInformation("Starting scrape for host {host}", ev.RemoteHost.Name);
    }
    public void OnScrapeHostEnd(ScrapeHostEvent ev) {
        _logger.LogInformation("Ended scrape for host {host}", ev.RemoteHost.Name);
    }
    public void OnScrapeHostFail(ScrapeHostFailEvent ev) {
        _logger.LogInformation("Failed scrape for host {host} with message: {message}", ev.RemoteHost.Name, ev.Message);
    }
}