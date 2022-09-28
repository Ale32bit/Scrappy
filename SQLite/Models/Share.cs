using Scrappy.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#nullable disable
namespace SQLite.Models;
public class Share : RemoteShare
{
    public int Id { get; set; }
    public int HostId { get; set; }
    public string IgnoreList { get; set; } = "";
    [NotMapped]
    public new IList<string> Ignore
    {
        get
        {
            if (string.IsNullOrEmpty(IgnoreList))
                return new List<string>();
            return IgnoreList.Split(';').ToList();
        }
    }
    public virtual Host Host { get; set; }
}
