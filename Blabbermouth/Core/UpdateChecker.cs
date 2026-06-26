using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Blabbermouth.Windows;
using DialogHostAvalonia;

namespace Blabbermouth.Core;

public static class UpdateChecker
{
    private const string UpdateUrl = "https://api.github.com/repos/PurelyAndy/Blabbermouth/releases/latest";
    private const string LatestVersionPage = "https://github.com/PurelyAndy/Blabbermouth/releases/latest";
    private static readonly HttpClient Client = new();
    public static async Task<bool> ShowUpdateNotificationIfAvailable(DialogHost dialog)
    {
        bool updateAvailable;
        string newVersion = "";
        string changelog = "";
        try
        {
            Client.DefaultRequestHeaders.UserAgent.ParseAdd("Blabbermouth Update Checker");
            HttpResponseMessage response = await Client.GetAsync(UpdateUrl);
            response.EnsureSuccessStatusCode();
            string content = await response.Content.ReadAsStringAsync();
            JsonElement root = JsonDocument.Parse(content).RootElement;
            newVersion = root.GetProperty("tag_name").GetString()![1..]; // Remove the leading 'v'
            changelog = root.GetProperty("body").GetString() ?? "";
            string? currentVersion = typeof(Program).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;
            currentVersion = string.IsNullOrEmpty(currentVersion) ? "0.0.0" : currentVersion.Split('+')[0];
            updateAvailable = new Version(newVersion) > new Version(currentVersion);
        }
        catch
        {
            updateAvailable = false;
        }

        if (updateAvailable)
        {
            object? result = await DialogHost.Show(new DialogBox(dialog,
                $"Version {newVersion} of Blabbermouth is available.\n" +
                        $"Please download it, it probably has important bugfixes.",
                "Please update.", "Open Download Page", "Show Changelog", "No"), dialog);

            if (result is "Open Download Page")
            {
                await OpenLatestVersionPage();
            }
            else if (result is "Show Changelog")
            {
                object? result2 = await DialogHost.Show(new DialogBox(dialog,
                    $"Version {newVersion} Changelog:\n\n{changelog}",
                    "Changelog", "Open Download Page", "No"), dialog);

                if (result2 is "Open Download Page")
                {
                    await OpenLatestVersionPage();
                }
            }
        }

        return updateAvailable;
    }

    public static async Task OpenLatestVersionPage()
    {
        await MainWindow.I.Launcher.LaunchUriAsync(new(LatestVersionPage));
    }
}