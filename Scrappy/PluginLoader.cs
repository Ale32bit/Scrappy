using Microsoft.Extensions.DependencyInjection;
using Scrappy.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Scrappy;
public class PluginLoader
{
    public const string PluginFolder = "Plugins";

    private static Assembly LoadPlugin(string fileName)
    {
        var path = Path.Combine(Environment.CurrentDirectory, fileName);

        var loadContext = new PluginLoadContext(path);
        return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(path)));
    }

    public static async Task<IEnumerable<IConnection>> LoadPlugins(IServiceProvider serviceProvider)
    {
        if (!Directory.Exists(PluginFolder))
            Directory.CreateDirectory(PluginFolder);
        
        var connections = new List<IConnection>();
        foreach(var fileName in Directory.GetFiles(PluginFolder).Where(q => q.EndsWith(".dll")))
        {
            var assembly = LoadPlugin(fileName);

            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(IConnection).IsAssignableFrom(type))
                {
                    IConnection result = ActivatorUtilities.CreateInstance(serviceProvider, type) as IConnection;
                    connections.Add(result);
                }
            }

        }

        // Pre init
        foreach(var conn in connections)
        {
            await conn.PreInit();
        }

        // Init
        foreach(var conn in connections)
        {
            await conn.Init();
        }

        return connections;
    }
}
