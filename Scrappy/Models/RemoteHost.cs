using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

#nullable disable

namespace Scrappy.Models;
public class RemoteHost
{
    public string Name { get; set; }
    public string Address { get; set; }
    public bool Legacy { get; set; } = false;
    public RemoteUser User { get; set; }
    public IEnumerable<RemoteShare> Shares { get; set; }
}
