using Scrappy.Connections;
using Scrappy.PluginLoader.PluginLoader;
using System.Reflection;

namespace Scrappy.PluginLoader;
public class Loader
{
    public const string PluginFolder = "Plugins";

    private static Assembly LoadPlugin(string fileName)
    {
        var path = Path.Combine(Environment.CurrentDirectory, fileName);

        var loadContext = new LoadContext(path);
        return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(path)));
    }

    public static async Task<IEnumerable<IPlugin>> LoadPlugins(IServiceProvider serviceProvider)
    {
        if (!Directory.Exists(PluginFolder))
            Directory.CreateDirectory(PluginFolder);

        var plugins = new List<IPlugin>();
        foreach (var fileName in Directory.GetFiles(PluginFolder).Where(q => q.EndsWith(".dll")))
        {
            var assembly = LoadPlugin(fileName);

            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(IPlugin).IsAssignableFrom(type))
                {
                    IPlugin result = ActivatorUtilities.CreateInstance(serviceProvider, type) as IPlugin;
                    plugins.Add(result);
                }
            }

        }

        // Pre init
        foreach (var conn in plugins)
        {
            await conn.PreInit();
        }

        // Init
        foreach (var conn in plugins)
        {
            await conn.Init();
        }

        return plugins;
    }
}
