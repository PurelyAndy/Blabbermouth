using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Blabbermouth.Core;
using Blabbermouth.Data;
using Blabbermouth.Windows;

namespace Blabbermouth.Views.ModelSelection;

public sealed class SherpaOnnxModelSelector : ModelSelector
{
    private const SttKind Kind = SttKind.SherpaOnnx;
    
    public SherpaOnnxModelSelector()
    {
        Configure(
            header: "Select Model",
            customPathHint: "...or bring your own sherpa-onnx model",
            linkText: "(Models can be found here, only \"streaming\" models work)",
            linkUri: new("https://github.com/k2-fsa/sherpa-onnx/releases/tag/asr-models"));
        ModelSelect.ItemsSource = Models.Keys;
        ModelSelect.SelectedIndex = Models.Count > 0 ? 0 : -1;
    }

    protected override async Task ContinueClicked()
    {
        if (!string.IsNullOrWhiteSpace(CustomModelPathValue))
        {
            SttManager.Model = new("N/A", CustomModelPathValue, "N/A");
            SttManager.Kind = Kind;
            SttManager.ResetRecognizers();

            MainWindow.CloseDialog();
            return;
        }

        if (SelectedModelName is not { } selectedModel) return;

        DownloadableModel model = Models[selectedModel];

        ContinueButton.IsEnabled = false;
        if (!model.IsPresent)
        {
            DownloadProgressBar.Value = 0;
            try
            {
                await model.DownloadAndExtractAsync(progress =>
                    { Dispatcher.UIThread.Post(() => { DownloadProgressBar.Value = progress * 100; }); });
            }
            catch (Exception ex)
            {
                await MainWindow.ShowErrorAsync(
                    $"Failed to download and extract the model:\n{ex}",
                    "Error");
                ContinueButton.IsEnabled = true;
                return;
            }
        }

        SttManager.Model = new("N/A", model.DirectoryPath, "N/A");
        SttManager.Kind = Kind;
        SttManager.ResetRecognizers();

        ContinueButton.IsEnabled = true;
        MainWindow.CloseDialog();
    }
    
    #region Model download links
    protected override Dictionary<string, DownloadableModel> Models => new()
    {
        ["English (sherpa-onnx-streaming-zipformer-en-kroko-2025-08-06)"] = new("sherpa-onnx-streaming-zipformer-en-kroko-2025-08-06",
            "https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-streaming-zipformer-en-kroko-2025-08-06.tar.bz2"),
        ["Deutsch/German (sherpa-onnx-streaming-zipformer-de-kroko-2025-08-06)"] = new("sherpa-onnx-streaming-zipformer-de-kroko-2025-08-06",
            "https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-streaming-zipformer-de-kroko-2025-08-06.tar.bz2"),
        ["Español/Spanish (sherpa-onnx-streaming-zipformer-es-kroko-2025-08-06)"] = new("sherpa-onnx-streaming-zipformer-es-kroko-2025-08-06",
            "https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-streaming-zipformer-es-kroko-2025-08-06.tar.bz2"),
        ["Français/French (sherpa-onnx-streaming-zipformer-fr-kroko-2025-08-06)"] = new("sherpa-onnx-streaming-zipformer-fr-kroko-2025-08-06",
            "https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-streaming-zipformer-fr-kroko-2025-08-06.tar.bz2"),
        ["English (sherpa-onnx-nemotron-speech-streaming-en-0.6b-1120ms-int8-2026-04-25)"] = new("sherpa-onnx-nemotron-speech-streaming-en-0.6b-1120ms-int8-2026-04-25",
            "https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-nemotron-speech-streaming-en-0.6b-1120ms-int8-2026-04-25.tar.bz2"),
    };
    #endregion
}
