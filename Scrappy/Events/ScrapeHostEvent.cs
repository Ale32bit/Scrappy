using Scrappy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scrappy.Events;

public class ScrapeHostEvent
{
    public RemoteHost RemoteHost { get; set; }
}
