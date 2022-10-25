using Scrappy.Models;

namespace Scrappy.Events;
#nullable disable
public class ScrapeHostFailEvent
{
    public RemoteHost RemoteHost { get; set; }
    public string Message { get; set; }
}
