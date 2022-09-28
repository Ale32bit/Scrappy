using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable disable

namespace Scrappy.Models;
public class RemoteShare
{
    public string Name { get; set; }
    public IList<string> Ignore { get; set; } = new List<string>();
    public bool AppendMode { get; set; } = false;

}
