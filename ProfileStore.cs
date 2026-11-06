using System.Text.Json;
using System.IO;

namespace SSHMonitor;

public sealed class JsonProfileRepository : IProfileRepository
{
    private readonly string _dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SSHMonitor");
    private string FilePath => Path.Combine(_dir, "profile.json");

    public ServerProfile Load()
    {
        try { return JsonSerializer.Deserialize<ServerProfile>(File.ReadAllText(FilePath), Options()) ?? new(); }
        catch { return new(); }
    }

    public void Save(ServerProfile profile)
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(profile, Options()));
    }

    private static JsonSerializerOptions Options() => new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
}
