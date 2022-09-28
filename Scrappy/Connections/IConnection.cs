using Scrappy.Models;

namespace Scrappy.Connections;
public interface IConnection : IDisposable
{
    public Task PreInit()
    {
        return Task.CompletedTask;
    }
    public Task Init()
    {
        return Task.CompletedTask;
    }
    public Task<IEnumerable<RemoteHost>> GetRemoteHostsAsync();
}
