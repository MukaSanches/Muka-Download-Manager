using System.Text.Json;
using System.Text.Json.Serialization;

namespace MukaDownloadManager.Core;

public sealed class AppSettings
{
    public string Language { get; set; } = "pt-BR";
    public string Theme { get; set; } = "Dark";
    public string Folder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    public int MaxSimultaneous { get; set; } = 3;
    public int Segments { get; set; } = 4;
    public int LimitKiB { get; set; } = 0;
    public bool StartAutomatically { get; set; } = true;
    public bool WatchClipboard { get; set; } = false;
}

public sealed class AppData
{
    public AppSettings Settings { get; set; } = new();
    public List<DownloadItem> Downloads { get; set; } = [];

    private static readonly object Gate = new();
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
    public static string DataFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MukaDownloadManager");
    public static string DataFile => Path.Combine(DataFolder, "state.json");

    public static AppData Load()
    {
        try
        {
            if (File.Exists(DataFile))
            {
                return JsonSerializer.Deserialize<AppData>(File.ReadAllText(DataFile), Options) ?? new();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Could not load saved state: " + ex.Message);
        }
        return new();
    }

    public void Save()
    {
        lock (Gate)
        {
            Directory.CreateDirectory(DataFolder);
            var staging = DataFile + ".tmp";
            var json = JsonSerializer.Serialize(this, Options);
            File.WriteAllText(staging, json);
            File.Move(staging, DataFile, true);
        }
    }
}
