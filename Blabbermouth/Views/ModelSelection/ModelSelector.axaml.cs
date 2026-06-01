using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Blabbermouth.Core;
using Blabbermouth.Data;

namespace Blabbermouth.Views.ModelSelection;

public partial class ModelSelector : UserControl
{
    protected string? SelectedModelName => ModelSelect.SelectedItem as string;
    protected string CustomModelPathValue => CustomModelPath.Text ?? string.Empty;

    protected ModelSelector()
    {
        InitializeComponent();
        SubheaderText.IsVisible = false;
        CustomPathHintText.IsVisible = false;
    }

    protected virtual async Task ContinueClicked(){}

    private async void ContinueButtonClicked(object? sender, RoutedEventArgs e)
    {
        await ContinueClicked();
    }

    protected void Configure(string header, string? subheader = null, string? customPathHint = null,
        string? linkText = null, Uri? linkUri = null)
    {
        HeaderText.Text = header;

        SubheaderText.Text = subheader ?? string.Empty;
        SubheaderText.IsVisible = !string.IsNullOrWhiteSpace(SubheaderText.Text);

        CustomPathHintText.Text = customPathHint ?? string.Empty;
        CustomPathHintText.IsVisible = !string.IsNullOrWhiteSpace(CustomPathHintText.Text);

        if (!string.IsNullOrWhiteSpace(linkText) && linkUri != null)
        {
            ModelsLink.Content = linkText;
            ModelsLink.NavigateUri = linkUri;
            ModelsLink.IsVisible = true;
        }
        else
        {
            ModelsLink.IsVisible = false;
        }
    }

    private void ModelSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        CustomModelPath.Text = string.Empty;
        if (SelectedModelName is not { } selectedModel) return;
        if (!Models.TryGetValue(selectedModel, out DownloadableModel? model))
            DownloadProgressBar.Value = 100;
        else
            DownloadProgressBar.Value = model.IsPresent ? 100 : 0;
    }

    private async void BrowseButtonClicked(object? sender, RoutedEventArgs e)
    {
        IStorageProvider? storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storageProvider == null) return;

        string lastLocation = Settings.Get<string>("lastLocation");
        IStorageFolder? startLocation = null;
        if (!string.IsNullOrWhiteSpace(lastLocation))
        {
            startLocation = await storageProvider.TryGetFolderFromPathAsync(lastLocation);
        }

        IReadOnlyList<IStorageFolder> result = await storageProvider.OpenFolderPickerAsync(new()
        {
            AllowMultiple = false,
            SuggestedStartLocation = startLocation,
        });

        if (result.Count <= 0) return;
        
        string selectedPath = result[0].Path.LocalPath;
        Settings.Set("lastLocation", selectedPath);
        CustomModelPath.Text = selectedPath;
        ModelSelect.SelectedItem = null;
    }

    protected virtual Dictionary<string, DownloadableModel> Models { get; }
}

