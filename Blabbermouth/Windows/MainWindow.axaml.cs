using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Blabbermouth.Core;
using Blabbermouth.Data;
using Blabbermouth.Util;
using Blabbermouth.Views;
using Blabbermouth.Views.ModelSelection;
using DialogHostAvalonia;
using ModelSelector = Blabbermouth.Views.ModelSelection.ModelSelector;
using Path = Avalonia.Controls.Shapes.Path;

namespace Blabbermouth.Windows;

public partial class MainWindow : Window
{
    public static MainWindow I { get; private set; } = null!;
    private readonly EmbeddedModelSelector _embeddedModelSelector = new();
    private readonly VoskModelSelector _voskModelSelector = new();
    private readonly SherpaOnnxModelSelector _sherpaOnnxModelSelector = new();

    public MainWindow()
    {
        InitializeComponent();
        I = this;
        
        if (!Directory.Exists(DownloadableModel.DownloadedModelsFolder)) Directory.CreateDirectory(DownloadableModel.DownloadedModelsFolder);
        if (!Directory.Exists(DownloadableModel.TempFolder)) Directory.CreateDirectory(DownloadableModel.TempFolder);
        
        ShockerConfig.SetUsingSerial(Settings.Get<bool>("usingSerial"));
    }

    private async void WindowLoaded(object? sender, RoutedEventArgs e)
    {
        if (OperatingSystem.IsWindows())
        {
            EmbeddedSpeechLocator.FindModels();
        }
        _embeddedModelSelector.UpdateModels();

        switch (SttManager.Kind)
        {
            case SttKind.Embedded:
                EmbeddedMenuOption.IsChecked = true;
                await ShowEmbeddedModelDialogAsync();
                break;
            case SttKind.Vosk:
                VoskMenuOption.IsChecked = true;
                await ShowVoskModelDialogAsync();
                break;
            case SttKind.SherpaOnnx:
                SherpaOnnxMenuOption.IsChecked = true;
                await ShowSherpaOnnxModelDialogAsync();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(SttManager.Kind), "Invalid STT kind in settings");
        }
    }

    private async Task ShowModelDialogAsync(ModelSelector selectorControl)
    {
        await DialogHost.Show(selectorControl, DialogNoClickAway);
    }

    private async Task ShowEmbeddedModelDialogAsync()
    {
        await ShowModelDialogAsync(_embeddedModelSelector);
    }

    private async Task ShowVoskModelDialogAsync()
    {
        await ShowModelDialogAsync(_voskModelSelector);
    }

    private async Task ShowSherpaOnnxModelDialogAsync()
    {
        await ShowModelDialogAsync(_sherpaOnnxModelSelector);
    }
    
    private void CollapseButtonClick(object? sender, RoutedEventArgs e)
    {
        CollapsableGrid.IsVisible = !CollapsableGrid.IsVisible;
        CollapseButton.Content = new Path
        {
            Data = CollapsableGrid.IsVisible
                ? Geometry.Parse("M 0 12 L 64 0 L 128 12")
                : Geometry.Parse("M 0 0 L 64 12 L 128 0"),
            Stroke = Brushes.White,
            StrokeThickness = 2,
            Stretch = Stretch.Fill,
        };
    }

    private async void StartStopButtonClicked(object? sender, RoutedEventArgs e)
    {
        SttManager.Enabled = !SttManager.Enabled;
        if (SttManager.Enabled)
        {
            if (!ShockerConfig.UsingSerial)
            {
                string? reason = await ShockerConfig.TestCredentialsAsync();
                if (reason != null)
                {
                    _ = DialogHost.Show(new DialogBox(Dialog, reason, "Cannot start Blabbermouth", "OK"), Dialog);
                    SttManager.Enabled = false;
                    return;
                }
            }
            else
            {
                await ShockerConfig.TestPortAsync();
                if (PiShock.SerialPort is not { IsOpen: true })
                {
                    SttManager.Enabled = false;
                    return;
                }
            }
            SttManager.UpdateRecognizers();
            StartStopButton.Content = "Stop Blabbermouth";
        }
        else
        {
            SttManager.DisposeRecognizers();
            StartStopButton.Content = "Start Blabbermouth";
        }
    }

    private void SaveCredentials(object? sender, RoutedEventArgs e)
    {
        Settings.Set<string>("username", ShockerConfig.Username);
        Settings.Set("shareCode", ShockerConfig.ShareCode);
        Settings.Set("apiKey", ShockerConfig.ApiKey);
    }

    private void ForgetCredentials(object? sender, RoutedEventArgs e)
    {
        Settings.Set("username", "");
        Settings.Set("shareCode", "");
        Settings.Set("apiKey", "");
    }

    private async void ExportPhrases(object? sender, RoutedEventArgs e)
    {
        IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new()
        {
            SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(Settings.Get<string>("lastLocation")),
            FileTypeChoices =
            [
                new("JSON")
                {
                    Patterns = ["*.json"],
                },
            ],
        });

        if (file == null) return;
        
        Settings.Set("lastLocation", System.IO.Path.GetDirectoryName(file.Path.LocalPath));
        string json = JsonSerializer.Serialize(PhraseList.Phrases, PhraseListJsonContext.Default.ListPhraseEntry);
        await File.WriteAllTextAsync(file.Path.LocalPath, json);
    }

    private async void ImportPhrases(object? sender, RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFile?> files = await StorageProvider.OpenFilePickerAsync(new()
        {
            SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(Settings.Get<string>("lastLocation")),
            AllowMultiple = false,
            FileTypeFilter =
            [
                new("JSON")
                {
                    Patterns = ["*.json"],
                },
            ],
        });

        if (files.Count <= 0 || files[0] == null) return;
        
        IStorageFile file = files[0]!;
        Settings.Set("lastLocation", System.IO.Path.GetDirectoryName(file.Path.LocalPath));
        string json = await File.ReadAllTextAsync(file.Path.LocalPath);
            
        try
        {
            PhraseList.ImportPhrases(json);
        }
        catch (JsonException)
        {
            await ShowErrorAsync(
                "The selected file does not contain valid phrase data.\n" +
                "Please select a different file or ensure the file is correctly formatted.",
                "Failed to import phrases");
        }
    }

    private void EmbeddedSpeechModeSelected(object? sender, RoutedEventArgs e)
    {
        _ = ShowEmbeddedModelDialogAsync();
    }

    private void VoskModeSelected(object? sender, RoutedEventArgs e)
    {
        _ = ShowVoskModelDialogAsync();
    }

    private void SherpaOnnxModeSelected(object? sender, RoutedEventArgs e)
    {
        _ = ShowSherpaOnnxModelDialogAsync();
    }

    private void ShowLicenses(object? sender, RoutedEventArgs e)
    {
        Button close = new() { Content = "Close" };
        close.Click += (_, _) => Dialog.CloseDialogCommand.Execute(null);
        
        WrapPanel top = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        StackPanel panel = new() { Margin = new(10) };
        int i = 0;
        foreach (KeyValuePair<string, string> keyValuePair in Licenses.LicenseDict)
        {
            StackPanel group = new()
            {
                Background = new SolidColorBrush(Color.FromArgb(
                    (byte)(255 * (i++ % 2 == 0 ? 0.1 : 0.2)), 255, 255, 255)),
                Margin = new(0, 0, 0, 20),
            };
            TextBlock name = new()
            {
                Text = keyValuePair.Key,
                FontWeight = FontWeight.Bold,
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            TextBlock license = new()
            {
                Text = keyValuePair.Value,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            group.Children.Add(name);
            group.Children.Add(license);
            panel.Children.Add(group);
            Button scrollToButton = new()
            {
                Content = keyValuePair.Key,
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new(1),
            };
            scrollToButton.Click += (_, _) =>
            {
                group.BringIntoView();
            };
            top.Children.Add(scrollToButton);
        }
        ScrollViewer scroll = new()
        {
            Content = panel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 320,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        Grid content = new();
        content.RowDefinitions.Add(new(GridLength.Auto));
        content.RowDefinitions.Add(new(GridLength.Auto));
        content.RowDefinitions.Add(new(GridLength.Auto));
        content.Children.Add(top);
        content.Children.Add(scroll);
        content.Children.Add(close);
        Grid.SetRow(top, 0);
        Grid.SetRow(scroll, 1);
        Grid.SetRow(close, 2);
        DialogHost.Show(content, Dialog);
    }

    private async void AboutClicked(object? sender, RoutedEventArgs e)
    {
        await DialogHost.Show(new AboutView(Dialog), Dialog);
    }
    
    public static async Task ShowErrorAsync(string message, string title)
    {
        await DialogHost.Show(new DialogBox(I.Dialog, message, title, "OK"), I.Dialog);
    }
    
    public static void CloseDialog()
    {
        I.DialogNoClickAway.CloseDialogCommand.Execute(null);
    }
}

[JsonSerializable(typeof(List<PhraseEntry>))]
public partial class PhraseListJsonContext : JsonSerializerContext;