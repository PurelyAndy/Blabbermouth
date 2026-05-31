using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace Blabbermouth.Views;

public partial class ModelSelectorView : UserControl
{
    public ModelSelectorView()
    {
        InitializeComponent();
        SubheaderText.IsVisible = false;
        CustomPathHintText.IsVisible = false;
    }

    public event EventHandler<SelectionChangedEventArgs>? ModelSelectionChangedEvent;

    public string? SelectedModelName => ModelSelect.SelectedItem as string;
    public string CustomModelPathValue => CustomModelPath.Text ?? string.Empty;

    public void Configure(string header, string? subheader = null, string? customPathHint = null,
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

    public void SetModelNames(IEnumerable<string> modelNames)
    {
        List<string> names = modelNames.ToList();
        ModelSelect.ItemsSource = names;
        ModelSelect.SelectedIndex = names.Count > 0 ? 0 : -1;
    }

    public void SetProgressVisible(bool isVisible)
    {
        DownloadProgressBar.IsVisible = isVisible;
    }

    public void SetProgress(double value)
    {
        DownloadProgressBar.Value = value;
    }

    public void ConfigureProgressText(bool showText, string? format = null)
    {
        DownloadProgressBar.ShowProgressText = showText;
        if (!string.IsNullOrWhiteSpace(format))
        {
            DownloadProgressBar.ProgressTextFormat = format;
        }
    }

    private void ModelSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        CustomModelPath.Text = string.Empty;
        ModelSelectionChangedEvent?.Invoke(this, e);
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
}

