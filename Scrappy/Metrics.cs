using Prometheus;
using Scrappy.Models;

namespace Scrappy;
public class Metrics
{
    public const string PREFIX = "scrappy_";

    public enum HostStatus
    {
        Offline,
        Errored,
        Online,
    }

    public static readonly Counter ComparedFilesCounter = Prometheus.Metrics.CreateCounter(PREFIX + "compared_files", "Amount of compared files", new CounterConfiguration()
    {
        LabelNames = new[] { "hostname", "name" }
    });

    public static readonly Counter DownloadedFilesCounter = Prometheus.Metrics.CreateCounter(PREFIX + "downloaded_files", "Amount of downloaded files", new CounterConfiguration()
    {
        LabelNames = new[] { "hostname", "name" }
    });

    public static readonly Counter DownloadedBytesCounter = Prometheus.Metrics.CreateCounter(PREFIX + "downloaded_bytes", "Amount of compared bytes", new CounterConfiguration()
    {
        LabelNames = new[] { "hostname", "name" }
    });

    public static readonly Gauge HostsGauge = Prometheus.Metrics.CreateGauge(PREFIX + "hosts", "Hosts status", new GaugeConfiguration
    {
        LabelNames = new[]
        {
            "hostname",
            "name"
        }
    });


    public static void SetHostStatus(HostStatus status, RemoteHost host)
    {
        HostsGauge.WithLabels(host.Address, host.Name).Set((int)status);
    }
}
