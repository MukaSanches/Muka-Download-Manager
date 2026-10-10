using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace MukaDownloadManager.Core;

public enum DownloadStatus { Queued, Downloading, Paused, Completed, Failed }

public sealed class DownloadItem : INotifyPropertyChanged
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Url { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string Name { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public string Error { get; set; } = "";

    private DownloadStatus _status = DownloadStatus.Queued;
    private long _downloadedBytes;
    private long _totalBytes;
    private double _speedBytes;
    private int _connections;

    public DownloadStatus Status { get => _status; set { Set(ref _status, value); Notify(nameof(StatusLabel)); Notify(nameof(IsInProgress)); } }
    public long DownloadedBytes { get => _downloadedBytes; set { Set(ref _downloadedBytes, value); Notify(nameof(Progress)); Notify(nameof(ProgressLabel)); Notify(nameof(SizeLabel)); } }
    public long TotalBytes { get => _totalBytes; set { Set(ref _totalBytes, value); Notify(nameof(Progress)); Notify(nameof(ProgressLabel)); Notify(nameof(SizeLabel)); } }
    public double SpeedBytes { get => _speedBytes; set { Set(ref _speedBytes, value); Notify(nameof(SpeedLabel)); } }
    public int Connections { get => _connections; set => Set(ref _connections, value); }

    [JsonIgnore] public double Progress => TotalBytes > 0 ? Math.Clamp(100.0 * DownloadedBytes / TotalBytes, 0, 100) : 0;
    [JsonIgnore] public string ProgressLabel => TotalBytes > 0 ? $"{Progress:0.0}%" : "—";
    [JsonIgnore] public string SizeLabel => TotalBytes > 0 ? $"{Size(DownloadedBytes)} / {Size(TotalBytes)}" : Size(DownloadedBytes);
    [JsonIgnore] public string SpeedLabel => Status == DownloadStatus.Downloading ? $"{Size((long)SpeedBytes)}/s" : "—";
    [JsonIgnore] public string StatusLabel => Localization.I18n.T("status." + Status.ToString().ToLowerInvariant());
    [JsonIgnore] public bool IsInProgress => Status == DownloadStatus.Downloading;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        Notify(name);
    }
    public void Notify(string? property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    public void RefreshLabels()
    {
        Notify(nameof(StatusLabel));
        Notify(nameof(SizeLabel));
        Notify(nameof(SpeedLabel));
    }
    public static string Size(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double number = Math.Max(0, bytes);
        int i = 0;
        while (number >= 1024 && i < units.Length - 1) { number /= 1024; i++; }
        return $"{number:0.#} {units[i]}";
    }
}
