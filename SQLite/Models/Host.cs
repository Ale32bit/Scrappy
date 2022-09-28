using Scrappy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

#nullable disable

namespace SQLite.Models;
public class Host : RemoteHost
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public virtual User User { get; set; }
    public virtual ICollection<Share> Shares { get; set; }
}
