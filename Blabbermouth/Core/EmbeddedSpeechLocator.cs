using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Blabbermouth.Core;

public static class EmbeddedSpeechLocator
{
    public static void FindModels()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                string windowsAppsFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "WindowsApps");
                if (!Directory.Exists(windowsAppsFolder))
                    return;
            
                List<string> speechFolders = Directory.EnumerateDirectories(windowsAppsFolder)
                    .Where(d => new DirectoryInfo(d).Name.StartsWith("MicrosoftWindows.Speech."))
                    .ToList();
    
                if (speechFolders.Count > 0)
                {
                    foreach (string folder in speechFolders)
                    {
                        string[] parts = new DirectoryInfo(folder).Name.Split('.');
                        if (parts.Length < 3) continue;
                    
                        string language = parts[2] + " (source: Windows)";
                        UseIfComprehensible(folder, language);
                    }
                }
            }
        }
        catch (Exception ignored)
        {
            // normal people haven't `takeown`'d their WindowsApps folder. it's not a big deal but it makes me sad.
        }
        
        List<string> vsCodeSpeechFolders = [];
        
        string vsCodeExtensionsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".vscode",
            "extensions");
        if (Directory.Exists(vsCodeExtensionsFolder))
        {
            vsCodeSpeechFolders.AddRange(Directory.EnumerateDirectories(vsCodeExtensionsFolder)
                .Where(d => new DirectoryInfo(d).Name.StartsWith("ms-vscode.vscode-speech-"))
                .ToList());
        }
            
        string insidersPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".vscode-insiders",
            "extensions");
        if (Directory.Exists(insidersPath))
        {
            vsCodeSpeechFolders.AddRange(Directory.EnumerateDirectories(insidersPath)
                .Where(d => new DirectoryInfo(d).Name.StartsWith("ms-vscode.vscode-speech-"))
                .ToList());
        }
        
        foreach (string folder in vsCodeSpeechFolders)
        {
            string modelPath = Path.Combine(folder, "assets", "stt");
            if (!Directory.Exists(modelPath)) continue;
            
            foreach (string subfolder in Directory.GetDirectories(modelPath))
            {
                string[] parts = new DirectoryInfo(subfolder).Name.Split('.');
                if (parts.Length < 4) continue;
                    
                string language = parts[1] + " (source: VS Code)";
                UseIfComprehensible(subfolder, language);
            }
        }
    }

    private static void UseIfComprehensible(string folder, string language)
    {
        string? eula = GetEula(folder);
        if (eula == null)
            return;

        string? version = GetVersion(folder);
        if (version == null)
            return;

        SttManager.SpeechModels[version.Split(' ')[^1] + " " + language] = new()
        {
            Eula = eula,
            Path = folder,
            Version = version,
        };
    }

    public static string? GetEula(string folder)
    {
        const int maxBytesToRead = 1000;
        
        string eulaContainingFile = Path.Combine(folder, "joint.onnx");
        if (!File.Exists(eulaContainingFile))
        {
            eulaContainingFile = Path.Combine(folder, "onnx", "joint_quantized.onnx");
        }

        if (!File.Exists(eulaContainingFile))
            return null;

        using FileStream stream = File.OpenRead(eulaContainingFile);

        byte[] buffer = new byte[Math.Min(maxBytesToRead, (int)stream.Length)];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);

        string text = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        const string eulaEndMarker = "others to use.";

        int eulaEndIndex = text.IndexOf(eulaEndMarker, StringComparison.Ordinal);
        if (eulaEndIndex == -1)
            return null;

        return text[..(eulaEndIndex + eulaEndMarker.Length)];
    }

    public static string? GetVersion(string folder)
    {
        string configPath = Path.Combine(folder, "lp.config");

        if (!File.Exists(configPath))
            return null;

        return File.ReadAllText(configPath)
            .Split('\n')
            .FirstOrDefault(l => l.StartsWith("name="))?
            .Split('=')[1]
            .Trim();
    }
}

