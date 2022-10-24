using Scrappy.Models;

namespace Scrappy.Events;

public class FileDownloadEvent
{
    public RemoteHost RemoteHost { get; set; }
    public RemoteShare RemoteShare { get; set; }
    public string SourceFilePath { get; set; }
}
