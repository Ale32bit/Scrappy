using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace Scrappy;

public class Utils
{
    public static bool Matches(string str, string filter)
    {
        var reg = "^" + Regex.Escape(str).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(filter, reg);
    }

    public static bool IsFiltered(string str, IEnumerable<string> filters)
    {
        foreach (var filter in filters)
        {
            if (Matches(str, filter))
                return true;
        }
        return false;
    }

    public static async Task<bool> IsHostOnlineAsync(IPAddress host, int timeout = 1000)
    {
        using var ping = new Ping();
        var result = await ping.SendPingAsync(host, timeout);
        return result.Status == IPStatus.Success;
    }

    public static async Task<IPAddress> ResolveName(string hostname)
    {
        try
        {
            if (IPAddress.TryParse(hostname, out var ipAddress))
                return ipAddress;

            var entries = await Dns.GetHostAddressesAsync(hostname, System.Net.Sockets.AddressFamily.InterNetwork);

            if (entries.Length == 0)
                return IPAddress.None;

            return entries[0];
        }
        catch
        {
            return IPAddress.None;
        }
    }
}
