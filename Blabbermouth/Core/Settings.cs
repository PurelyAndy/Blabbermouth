using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Blabbermouth.Data;

namespace Blabbermouth.Core;

public static class Settings
{
    private const string SettingsPath = "settings.csv";
    private static List<string> _lines;
    private static readonly Lock Lock = new();

    public static T Set<T>(string setting, T value)
    {
        lock (Lock)
        {
            _lines.RemoveAll(line => line.StartsWith(setting + ","));
            _lines.Add(setting + ',' + value);
            File.WriteAllLines(SettingsPath, _lines);
            return value;
        }
    }

    public static T? Get<T>(string setting)
    {
        lock (Lock)
        {
            foreach (string line in _lines)
            {
                if (!line.StartsWith(setting + ",")) continue;

                string[] split = line.Split(',', 2);
                Type type = typeof(T);

                if (string.IsNullOrEmpty(split[1]))
                {
                    return type == typeof(string) ? (T)(object)"": default!;
                }

                object value = type.IsEnum ? Enum.Parse(type, split[1]) : Convert.ChangeType(split[1], type);
                return (T)value;
            }
            return default;
        }
    }

    private static void Remove(string setting)
    {
        lock (Lock)
        {
            if (_lines.RemoveAll(line => line.StartsWith(setting + ",")) > 0)
            {
                File.WriteAllLines(SettingsPath, _lines);
            }
        }
    }

    private static bool Has(string setting)
    {
        lock (Lock)
        {
            return _lines.Any(line => line.StartsWith(setting + ","));
        }
    }

    static Settings()
    {
        if (!File.Exists(SettingsPath))
        {
            File.Create(SettingsPath).Close();
            _lines = [];
        }
        else
        {
            _lines = File.ReadAllLines(SettingsPath).ToList();
            Update();
        }

        if (!Has("version"))                    Set("version",                      "2");
        if (!Has("shockers"))                   Set("shockers",                     "[]");
        if (!Has("serialPort"))                 Set("serialPort",                   "");
        if (!Has("mode"))                       Set("mode",                         SttKind.Embedded);
        if (!Has("lastLocation"))               Set("lastLocation",                 Directory.GetCurrentDirectory());
        if (!Has("micDevice"))                  Set("micDevice",                    "");
        if (!Has("speakerDevice"))              Set("speakerDevice",                "");
        if (!Has("useMic"))                     Set("useMic",                       false);
        if (!Has("useSpeaker"))                 Set("useSpeaker",                   false);
        if (!Has("lastPhrases"))                Set("lastPhrases",                  "[]");
        if (!Has("lastModel"))                  Set("lastModel",                    "");
        if (!Has("lastModelWasCustom"))         Set("lastModelWasCustom",           false);
        if (!Has("allowMultiplePhrases"))       Set("allowMultiplePhrases",         true);
        if (!Has("allowMultipleOfSamePhrase"))  Set("allowMultipleOfSamePhrase",    false);
        if (!Has("detectBeforeDoneTalking"))    Set("detectBeforeDoneTalking",      false);
    }

    private static void Update()
    {
        Version0To1();
        Version1To2();
    }

    private static void Version0To1()
    {
        if (_lines.Any(l => l.StartsWith("version,"))) return;
        if (!_lines.Any(l => l.Split(',').Length > 1 && l.Split(',', 3).Length < 3)) return;

        _lines = _lines.Select(line =>
        {
            string[] split = line.Split(',', 3);
            if (split.Length < 3)
                return line;
            return split[0] + "," + split[2];
        }).ToList();

        Set("version", 1);
    }

    private static void Version1To2()
    {
        if (Get<int>("version") >= 2) return;

        string? shareCode = Get<string>("shareCode");
        string? apiKey = Get<string>("apiKey");
        string? shockerId = Get<string>("shockerId");
        bool usingSerial = Get<bool>("usingSerial") && !string.IsNullOrEmpty(shockerId) && OperatingSystem.IsWindows();

        List<Shocker> shockers;
        if (!usingSerial && (string.IsNullOrEmpty(shareCode) || string.IsNullOrEmpty(apiKey)))
        {
            shockers = [];
        }
        else
        {
            shockers =
            [
                usingSerial
                    ? new(shockerId)
                    : new(shareCode, apiKey),
            ];
        }

        string shockersJson = JsonSerializer.Serialize(shockers, JsonContext.Default.ListShocker);
        Set("shockers", shockersJson);
        Remove("username");
        Remove("shareCode");
        Remove("apiKey");
        Remove("shockerId");
        Remove("usingSerial");

        Set("version", 2);
    }
}