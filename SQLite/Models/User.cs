using Scrappy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable disable
namespace SQLite.Models;

public class User : RemoteUser
{
    public int Id { get; set; }
    public virtual ICollection<Host> Hosts { get; set; }
}
