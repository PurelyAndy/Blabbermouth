using System;
using System.IO;
using System.Linq;
using Blabbermouth.Data;
using Blabbermouth.Util;

namespace Blabbermouth.Core;

public static class Settings
{
    private const string SettingsPath = "settings.csv";
    
    public static void Set<T>(string setting, T value)
    {
        string[] lines = File.ReadAllLines(SettingsPath);
        File.WriteAllLines(SettingsPath, lines.Select(line =>
        {
            if (line.StartsWith(setting + ","))
                return setting + "," + value;
            return line;
        }));
    }

    public static T? Get<T>(string setting)
    {
        string[] lines = File.ReadAllLines(SettingsPath);
        foreach (string line in lines)
        {
            if (line.StartsWith(setting + ","))
            {
                string[] split = line.Split(',', 2);
                Type type = typeof(T);
                
                if (string.IsNullOrEmpty(split[1]))
                {
                    return type == typeof(string) ? (T)(object)"": default!;
                }
                else
                {
                    object value = type.IsEnum ? Enum.Parse(type, split[1]) : Convert.ChangeType(split[1], type);
                    return (T)value;
                }
            }
        }

        return default;
    }

    private static void Add<T>(string setting, T value) => File.AppendAllText(SettingsPath, '\n' + setting + ',' + value);

    private static bool Contains(string setting)
    {
        string[] lines = File.ReadAllLines(SettingsPath);
        return lines.Any(line => line.StartsWith(setting + ","));
    }

    static Settings()
    {
        if (!File.Exists(SettingsPath))
            File.Create(SettingsPath).Close();
        else
        {
            string[] lines = File.ReadAllLines(SettingsPath);
            if (lines.Any(l => l.Split(',').Length > 1 && l.Split(',', 3).Length < 3))
            {
                File.WriteAllLines(SettingsPath, lines.Select(line =>
                {
                    string[] split = line.Split(',', 3);
                    if (split.Length < 3)
                        return line;
                    return split[0] + "," + split[2];
                }));
            }
        }
        
        if (!Contains("version"))
            Add("version", "1");
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
        if (!Contains("micDevice"))
            Add("micDevice", "");
        if (!Contains("speakerDevice"))
            Add("speakerDevice", "");
        if (!Contains("speakerDevice"))
            Add("lastPhrases", "[]");
        if (!Contains("lastModel"))
            Add("lastModel", "");
    }
}