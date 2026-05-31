using System;
using System.IO;
using System.Linq;

namespace Blabbermouth;

public static class Settings
{
    private const string SettingsPath = "settings.csv";
    
    public static void Set<T>(string setting, T value)
    {
        string[] lines = File.ReadAllLines(SettingsPath);
        File.WriteAllLines(SettingsPath, lines.Select(line =>
        {
            if (line.StartsWith(setting + "," + typeof(T).FullName + ","))
                return setting + "," + typeof(T).FullName + "," + value;
            return line;
        }));
    }

    public static T Get<T>(string setting) => CsvParser.Parse(File.ReadAllLines(SettingsPath))[setting];

    private static void Add<T>(string setting, T value) => File.AppendAllText(SettingsPath, '\n' + setting + ',' + typeof(T).FullName + ',' + value);

    private static bool Contains(string setting) => CsvParser.Parse(File.ReadAllLines(SettingsPath)).ContainsKey(setting);

    static Settings()
    {
        if (!File.Exists(SettingsPath))
            File.Create(SettingsPath).Close();
        
        if (!Contains("username"))
            Add("username", "");
        if (!Contains("shareCode"))
            Add("shareCode", "");
        if (!Contains("apiKey"))
            Add("apiKey", "");
        if (!Contains("mode"))
            Add("mode", SttKind.Embedded);
        if (!Contains("lastLocation"))
            Add("lastLocation", Directory.GetCurrentDirectory());
        if (!Contains("shockerId"))
            Add("shockerId", "");
        if (!Contains("usingSerial"))
            Add("usingSerial", OperatingSystem.IsWindows());
    }
}