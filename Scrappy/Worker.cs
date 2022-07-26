using Scrappy.PluginLoader;
using Scrappy.Events;
using Scrappy.Models;
using SMBLibrary;
using SMBLibrary.Client;
using static Scrappy.Metrics;
using System.Collections.Concurrent;

namespace Scrappy;
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public IEnumerable<IPlugin> Plugins { get; private set; }
    private readonly ConcurrentQueue<RemoteHost> HostsQueue = new();
    // name+address:share/filePath = dateTime
    private readonly ConcurrentDictionary<string, DateTime> LocalFilesCache = new();

    public long MaxFileSize;

    public static readonly string[] IgnoredPaths = {
        ".",
        "..",
    };

    public Worker(ILogger<Worker> logger, IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    private DateTime GetLastFileWrite(RemoteHost host, RemoteShare share, params string[] pathParts)
    {
        var filePath = Path.Combine(pathParts);
        var key = $"{host.Name}+{host.Address}:{share.Name}/{filePath}";
        if (!LocalFilesCache.TryGetValue(key, out DateTime dateTime))
        {
            var fullOutputPath = Path.Combine(_configuration["OutputDirectoryPath"], host.Name, share.Name, filePath);
            dateTime = File.GetLastWriteTimeUtc(fullOutputPath);
            LocalFilesCache[key] = dateTime;
        }

        return dateTime;
    }

    private void UpdateLastFileWrite(RemoteHost host, RemoteShare share, params string[] pathParts)
    {
        var filePath = Path.Combine(pathParts);
        var key = $"{host.Name}+{host.Address}:{share.Name}/{filePath}";
        LocalFilesCache[key] = DateTime.UtcNow;
    }

    private async Task<IList<RemoteHost>> FetchAllHostsAsync()
    {
        _logger.LogInformation("Fetching all hosts...");
        var hosts = new List<RemoteHost>();
        foreach (var plugin in Plugins)
        {
            var connHosts = await plugin.GetRemoteHostsAsync();
            hosts.AddRange(connHosts);
        }
        return hosts;
    }

    private async Task ScrapeHost(RemoteHost host)
    {
        try
        {
            _logger.LogDebug("Starting scrape for {host}", host.Name);

            var scrapeStartEvent = new ScrapeHostEvent
            {
                RemoteHost = host,
            };
            foreach (var plugin in Plugins)
                plugin.OnScrapeHostStart(scrapeStartEvent);

            var address = await Utils.ResolveName(host.Address);
            var isOnline = await Utils.IsHostOnlineAsync(address);

            if (!isOnline)
            {
                SetHostStatus(HostStatus.Offline, host);
                _logger.LogDebug("{host} is offline", host.Name);
                return;
            }

            ISMBClient smbClient = host.Legacy ? new SMB1Client() : new SMB2Client();

            var connected = smbClient.Connect(address, SMBTransportType.DirectTCPTransport);
            if (!connected)
            {
                connected = smbClient.Connect(address, SMBTransportType.NetBiosOverTCP);
                if (!connected)
                {
                    _logger.LogInformation("Could not connect to host {host.Address} ({host.Name})", host.Address, host.Name);
                    SetHostStatus(HostStatus.Errored, host);

                    var hostFailEvent = new ScrapeHostFailEvent
                    {
                        RemoteHost = host,
                        Message = "Connection failure",
                    };
                    foreach (var plugin in Plugins)
                        plugin.OnScrapeHostFail(hostFailEvent);

                    return;
                }
            }

            SetHostStatus(HostStatus.Online, host);

            var loginStatus = smbClient.Login(host.User.Domain, host.User.Username, host.User.Password);
            if (loginStatus != NTStatus.STATUS_SUCCESS)
            {
                _logger.LogWarning("Could not authenticate to host {host.Address} ({host.Name}): {loginStatus}. Skipping...", host.Address, host.Name, loginStatus);
                SetHostStatus(HostStatus.Errored, host);
                smbClient.Disconnect();
                var hostFailEvent = new ScrapeHostFailEvent
                {
                    RemoteHost = host,
                    Message = "Authentication failure",
                };
                foreach (var plugin in Plugins)
                    plugin.OnScrapeHostFail(hostFailEvent);

                return;
            }

            var hostShares = smbClient.ListShares(out var listStatus);
            if (listStatus != NTStatus.STATUS_SUCCESS)
            {
                _logger.LogError("Error getting list of shares for {host}. {status}", host.Name, listStatus);
                SetHostStatus(HostStatus.Errored, host);
                smbClient.Disconnect();
                var hostFailEvent = new ScrapeHostFailEvent
                {
                    RemoteHost = host,
                    Message = "Share list failure",
                };
                foreach (var plugin in Plugins)
                    plugin.OnScrapeHostFail(hostFailEvent);

                return;
            }

            foreach (var share in host.Shares)
            {
                if (!hostShares.Contains(share.Name))
                {
                    _logger.LogWarning("Host {host.Name} does not contain the share {share.Name}", host.Name, share.Name);
                    SetHostStatus(HostStatus.Errored, host);
                    continue;
                }

                var shareStore = smbClient.TreeConnect(share.Name, out var status);
                if (status != NTStatus.STATUS_SUCCESS)
                {
                    _logger.LogError("{status}", status.ToString());
                    SetHostStatus(HostStatus.Errored, host);
                    continue;
                }

                var basePath = host.Legacy ? @"\\" : "";

                RecursiveCopy(basePath, true, shareStore, host, share);

            }

            smbClient.Disconnect();


        }
        catch (Exception e)
        {
            _logger.LogError(exception: e, message: "Exception in ScrapeHost");
            var hostFailEvent = new ScrapeHostFailEvent
            {
                RemoteHost = host,
                Message = e.Message,
            };
            foreach (var plugin in Plugins)
                plugin.OnScrapeHostFail(hostFailEvent);
        }
        finally
        {
            var scrapeHostEndEvent = new ScrapeHostEvent
            {
                RemoteHost = host,
            };
            foreach (var plugin in Plugins)
                plugin.OnScrapeHostEnd(scrapeHostEndEvent);
        }
    }

    private async Task Dispatch()
    {
        while (HostsQueue.TryDequeue(out var host))
        {
            await ScrapeHost(host);
        }
    }

    private void RecursiveCopy(string path, bool isDirectory, ISMBFileStore shareStore, RemoteHost host, RemoteShare share)
    {
        var status = shareStore.CreateFile(
            out var fileHandle,
            out var fileStatus,
            path,
            AccessMask.GENERIC_READ | (isDirectory ? 0 : AccessMask.SYNCHRONIZE),
            isDirectory ?
                SMBLibrary.FileAttributes.Directory :
                SMBLibrary.FileAttributes.Normal,
            ShareAccess.Read,
            CreateDisposition.FILE_OPEN,
            isDirectory ?
                CreateOptions.FILE_DIRECTORY_FILE :
                CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT | CreateOptions.FILE_OPEN_REMOTE_INSTANCE,
            null);

        if (status != NTStatus.STATUS_SUCCESS)
        {
            if (status == NTStatus.STATUS_SHARING_VIOLATION)
            {
                _logger.LogInformation("Error reading path {path}:{status} {fileStatus}. Is the file in use?", path, status, fileStatus);
            }
            else
            {
                _logger.LogError("Error reading path {path}:{status} {fileStatus}", path, status, fileStatus);
            }
            return;
        }

        if (isDirectory)
        {
            _logger.LogDebug("Directory {path}", path);
            _logger.LogTrace("{host}/{fileName}: fileHandle is {fileHandle}; shareStore is {shareStore}; CreateFile status is {status}", host.Name, path, fileHandle, shareStore, status);

            try
            {
                shareStore.QueryDirectory(out var fileList, fileHandle, shareStore is SMB1FileStore ? @"\\*" : "*", FileInformationClass.FileDirectoryInformation);
                shareStore.CloseFile(fileHandle);

                foreach (var fileQuery in fileList)
                {
                    var file = (FileDirectoryInformation)fileQuery;
                    if (IgnoredPaths.Contains(file.FileName))
                        continue;

                    var filename = Path.GetFileName(path);
                    if (Utils.IsFiltered(filename, share.Ignore))
                        continue;

                    ComparedFilesCounter.WithLabels(host.Address, host.Name).Inc();

                    var fullOutputPath = Path.Combine(_configuration["OutputDirectoryPath"], host.Name, share.Name, path, file.FileName);
                    if (file.FileAttributes != SMBLibrary.FileAttributes.Directory && File.Exists(fullOutputPath))
                    {
                        var lastLocalWriteDate = GetLastFileWrite(host, share, path, file.FileName);
                        if (file.LastWriteTime < lastLocalWriteDate)
                            continue;
                    }

                    RecursiveCopy(Path.Combine(path, file.FileName), file.FileAttributes.HasFlag(SMBLibrary.FileAttributes.Directory), shareStore, host, share);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(exception: e, message: "Error querying directory");
                return;
            }
        }
        else
        {
            _logger.LogDebug("File      {path}", path);

            shareStore.GetFileInformation(out var fileInfoBase, fileHandle, FileInformationClass.FileAllInformation);
            FileAllInformation fileInfo = (FileAllInformation)fileInfoBase;

            if (fileInfo.StandardInformation.AllocationSize > MaxFileSize)
            {
                _logger.LogWarning("File at {path} exceeds max file size!", path);
                return;
            }

            _logger.LogDebug("Starting download for file {host} / {fileName};", host.Name, path);

            var fullOutputPath = Path.Combine(_configuration["OutputDirectoryPath"], host.Name, share.Name, path);
            var outputPathDir = Path.GetDirectoryName(fullOutputPath)!;
            Directory.CreateDirectory(outputPathDir);

            var tempPath = Path.Combine(outputPathDir, Path.GetRandomFileName());

            var fileDownloadEvent = new FileDownloadEvent
            {
                RemoteHost = host,
                RemoteShare = share,
                SourceFilePath = path,
            };
            foreach (var plugin in Plugins)
                plugin.OnFileDownload(fileDownloadEvent);

            Stream stream;
            if (share.AppendMode && File.Exists(fullOutputPath))
                stream = new MemoryStream();
            else
                stream = File.Open(tempPath, FileMode.Create, FileAccess.Write, FileShare.Read);

            long bytesRead = 0;
            bool failed = false;
            while (true)
            {
                status = shareStore.ReadFile(out byte[] data, fileHandle, bytesRead, (int)shareStore.MaxReadSize);

                if (status != NTStatus.STATUS_SUCCESS && status != NTStatus.STATUS_END_OF_FILE)
                {
                    failed = true;
                    var hostFailEvent = new ScrapeHostFailEvent
                    {
                        RemoteHost = host,
                        Message = status.ToString(),
                    };
                    foreach (var plugin in Plugins)
                        plugin.OnScrapeHostFail(hostFailEvent);

                    _logger.LogError("Failed to read file {path}", path);
                }

                if (status == NTStatus.STATUS_END_OF_FILE || data == null || data.Length == 0)
                    break;

                bytesRead += data.Length;
                stream.Write(data, 0, data.Length);
                DownloadedBytesCounter.WithLabels(host.Address, host.Name).Inc(data.Length);

            }
            shareStore.CloseFile(fileHandle);

            _logger.LogDebug("Done download");

            if (failed)
                return;

            if (share.AppendMode && File.Exists(fullOutputPath))
                AppendOutputFile(path, stream, host, share);
            else
            {
                stream.Dispose();
                File.Move(tempPath, fullOutputPath, true);
            }

            UpdateLastFileWrite(host, share, path);

            var fileWriteEvent = new FileWriteEvent
            {
                RemoteHost = host,
                RemoteShare = share,
                SourceFilePath = path,
                DestinationFilePath = fullOutputPath,
                AppendMode = share.AppendMode,
            };
            foreach (var plugin in Plugins)
                plugin.OnFileWrite(fileWriteEvent);

            DownloadedFilesCounter.WithLabels(host.Address, host.Name).Inc();
        }
    }

    private void AppendOutputFile(string path, Stream stream, RemoteHost host, RemoteShare share)
    {
        var fullOutputPath = Path.Combine(_configuration["OutputDirectoryPath"], host.Name, share.Name, path);

        stream.Position = 0;

        var bufferSize = _configuration.GetValue("AppendBufferSize", 1024 * 1024);
        using var localStream = File.OpenText(fullOutputPath);
        long position = 0;
        bool lessThanSplitSize = localStream.BaseStream.Length < bufferSize;

        if (!lessThanSplitSize)
            position = localStream.BaseStream.Length - bufferSize;

        var buffer = new byte[bufferSize];
        localStream.BaseStream.Position = position;
        var localContent = localStream.ReadToEnd();
        localStream.Dispose();

        var memReader = new StreamReader(stream);
        var text = memReader.ReadToEnd();

        var diff = text.Split(localContent);

        if (diff.Length >= 2)
        {
            var content = diff.Last();
            File.AppendAllText(fullOutputPath, content);
        }
        else if (diff.Length == 1 && lessThanSplitSize)
        {
            File.AppendAllText(fullOutputPath, diff[0]);
        }
    }

    private CountdownEvent StartScraping(int threads)
    {
        CountdownEvent countdown = new(threads);
        for (int i = 0; i < threads; i++)
        {
            ThreadPool.QueueUserWorkItem(async _ =>
            {
                await Dispatch();
                countdown.Signal();
            });
        }

        return countdown;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Plugins = await Loader.LoadPlugins(_serviceProvider);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                MaxFileSize = _configuration.GetValue<long>("MaxFileSize", 1073741824);
                HostsQueue.Clear();
                var allHosts = await FetchAllHostsAsync();
                foreach (var host in allHosts)
                    HostsQueue.Enqueue(host);

                _logger.LogInformation("Starting all scrapers");

                foreach (var plugin in Plugins)
                    plugin.OnScrapeCycleStart();

                using var countdownEvent = StartScraping(_configuration.GetValue("Threads", 2));

                var isSet = await Task.Run(() => countdownEvent.Wait(TimeSpan.FromMinutes(_configuration.GetValue("CycleTimeout", 30))), stoppingToken);

                ScrapeCyclesCounter.Inc();

                if (!isSet)
                    foreach (var plugin in Plugins)
                        plugin.OnScrapeCycleTimeout();

                foreach (var plugin in Plugins)
                    plugin.OnScrapeCycleEnd();

                _logger.LogInformation("All threads ended");

                await Task.Delay(_configuration.GetValue("EndPause", 15) * 1000, stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogError(message: "Execution failure", exception: e); ;
            }
        }
    }
}