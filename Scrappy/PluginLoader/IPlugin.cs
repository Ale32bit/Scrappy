using Scrappy.Events;
using Scrappy.Models;

namespace Scrappy.PluginLoader;
public interface IPlugin
{
    public Task PreInit()
    {
        return Task.CompletedTask;
    }
    public Task Init()
    {
        return Task.CompletedTask;
    }
    public Task<IEnumerable<RemoteHost>> GetRemoteHostsAsync()
    {
        return Task.FromResult(Enumerable.Empty<RemoteHost>());
    }

    public void OnScrapeCycleStart() { }
    public void OnScrapeCycleEnd() { }
    public void OnScrapeCycleTimeout() { }
    public void OnFileDownload(FileDownloadEvent ev) { }
    public void OnFileWrite(FileWriteEvent ev) { }
    public void OnScrapeHostStart(ScrapeHostEvent ev) { }
    public void OnScrapeHostEnd(ScrapeHostEvent ev) { }
    public void OnScrapeHostFail(ScrapeHostFailEvent ev) { }
}
