using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable disable

namespace Scrappy.Models;
public class RemoteUser
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string Domain { get; set; } = "";
}
