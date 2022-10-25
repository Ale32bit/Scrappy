#nullable disable

namespace Scrappy.Models;
public class RemoteShare
{
    public string Name { get; set; }
    public IList<string> Ignore { get; set; } = new List<string>();
    public bool AppendMode { get; set; } = false;

}
