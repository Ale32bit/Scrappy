using Prometheus;
using Prometheus.DotNetRuntime;
using Scrappy;
using Scrappy.Logger;
using System.Globalization;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var logsPath = context.Configuration["LogsPath"];
        Directory.CreateDirectory(logsPath);
        var now = DateTime.Now;
        var logName = $"{now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}.log";
        var fileLoggerProvider = new FileLoggerProvider(Path.Combine(logsPath,logName));
        services.AddLogging(o =>
        {
            o.AddProvider(fileLoggerProvider);
        });

        services.AddHostedService<Worker>();

        var promConfig = context.Configuration.GetSection("Prometheus");
        if(promConfig.GetValue("Enabled", false))
        {
            var promServer = new KestrelMetricServer(promConfig.GetValue<int>("Port"));
            promServer.Start();
            DotNetRuntimeStatsBuilder.Default().StartCollecting();
        }
    })
    .Build();

await host.RunAsync();
