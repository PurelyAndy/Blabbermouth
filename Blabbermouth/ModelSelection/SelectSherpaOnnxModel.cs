using System;
using System.Collections.Generic;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Blabbermouth.ModelSelection;

public class SelectSherpaOnnxModelView : SelectVoskModelView
{
    private const SttKind Kind = SttKind.SherpaOnnx;
    
    public SelectSherpaOnnxModelView()
    {
        Selector.Configure(
            header: "Select Model",
            customPathHint: "...or bring your own sherpa-onnx model",
            linkText: "(Models can be found here, only \"streaming\" models work)",
            linkUri: new("https://github.com/k2-fsa/sherpa-onnx/releases/tag/asr-models"));
        Models = _sherpaOnnxModels;
        Selector.SetModelNames(Models.Keys);
        ContinueButton.Click -= ContinueButtonClicked;
        ContinueButton.Click += ContinueClicked;
    }

    private async void ContinueClicked(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(Selector.CustomModelPathValue))
        {
            SttManager.Model = new("N/A", Selector.CustomModelPathValue, "N/A");
            SttManager.Kind = Kind;
            SttManager.ResetRecognizers();

            Close();
            return;
        }

        if (Selector.SelectedModelName is not { } selectedModel) return;

        DownloadableModel model = _sherpaOnnxModels[selectedModel];

        ContinueButton.IsEnabled = false;
        if (!model.IsPresent)
        {
            Selector.SetProgress(0);
            try
            {
                await model.DownloadAndExtractAsync(progress =>
                    { Dispatcher.UIThread.Post(() => { Selector.SetProgress(progress * 100); }); });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(
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
        Close();
    }
    
    #region Model download links
    private readonly Dictionary<string, DownloadableModel> _sherpaOnnxModels = new()
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
