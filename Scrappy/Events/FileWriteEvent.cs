using Scrappy.Models;

namespace Scrappy.Events;

public class FileWriteEvent
{
    public RemoteHost RemoteHost { get; set; }
    public RemoteShare RemoteShare { get; set; }
    public string SourceFilePath { get; set; }
    public string DestinationFilePath { get; set; }
    public bool AppendMode { get; set; }
}
