using System;
using System.Collections.Generic;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Blabbermouth.ModelSelection;

public partial class SelectVoskModelView : SelectModelView
{
    private const SttKind Kind = SttKind.Vosk;

    public SelectVoskModelView()
    {
        InitializeComponent();
        Selector.Configure(
            header: "Select Model",
            customPathHint: "...or bring your own Vosk model",
            linkText: "(Models are available here)",
            linkUri: new("https://alphacephei.com/vosk/models"));
        Selector.SetModelNames(Models.Keys);
        Selector.SetProgressVisible(true);
        Selector.ConfigureProgressText(true, "Downloaded: {1:0}%");
        Selector.ModelSelectionChangedEvent += (_, _) => UpdateProgressForSelection();
        UpdateProgressForSelection();
    }

    protected async void ContinueButtonClicked(object? sender, RoutedEventArgs e)
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

        DownloadableModel model = Models[selectedModel];

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

    private void UpdateProgressForSelection()
    {
        if (Selector.SelectedModelName is not { } selectedModel) return;
        if (!Models.TryGetValue(selectedModel, out DownloadableModel? model))
            Selector.SetProgress(100);
        else
            Selector.SetProgress(model.IsPresent ? 100 : 0);
    }

    #region Model download links

    protected Dictionary<string, DownloadableModel> Models = new()
    {
        ["English Small (vosk-model-small-en-us-0.15)"] = new("vosk-model-small-en-us-0.15",
            "https://alphacephei.com/vosk/models/vosk-model-small-en-us-0.15.zip"),
        ["English Large (vosk-model-en-us-0.42-gigaspeech)"] = new("vosk-model-en-us-0.42-gigaspeech",
            "https://alphacephei.com/vosk/models/vosk-model-en-us-0.42-gigaspeech.zip"),

        ["Español/Spanish Small (vosk-model-small-es-0.42)"] = new("vosk-modelsmall-es-0.42",
            "https://alphacephei.com/vosk/models/vosk-model-small-es-0.42.zip"),
        ["Español/Spanish Large (vosk-model-es-0.42)"] = new("vosk-model-es-0.42",
            "https://alphacephei.com/vosk/models/vosk-model-es-0.42.zip"),

        ["Deutsch/German Small (vosk-model-small-de-0.15)"] = new("vosk-model-smallde-0.15",
            "https://alphacephei.com/vosk/models/vosk-model-small-de-0.15.zip"),
        ["Deutsch/German Large (vosk-model-de-0.21)"] = new("vosk-model-de-0.21",
            "https://alphacephei.com/vosk/models/vosk-model-de-0.21.zip"),

        ["Français/French Small (vosk-model-small-fr-0.22)"] = new("vosk-modelsmall-fr-0.22",
            "https://alphacephei.com/vosk/models/vosk-model-small-fr-0.22.zip"),
        ["Français/French Large (vosk-model-fr-0.22)"] = new("vosk-model-fr-0.22",
            "https://alphacephei.com/vosk/models/vosk-model-fr-0.22.zip"),

        ["русский/Russian Small (vosk-model-small-ru-0.22)"] = new("vosk-modelsmall-ru-0.22",
            "https://alphacephei.com/vosk/models/vosk-model-small-ru-0.22.zip"),
        ["русский/Russian Large (vosk-model-ru-0.42)"] = new("vosk-model-ru-0.42",
            "https://alphacephei.com/vosk/models/vosk-model-ru-0.42.zip"),

        ["汉语/Chinese Small (vosk-model-small-cn-0.22)"] = new("vosk-model-smallcn-0.22",
            "https://alphacephei.com/vosk/models/vosk-model-small-cn-0.22.zip"),
        ["汉语/Chinese Large (vosk-model-cn-0.22)"] = new("vosk-model-cn-0.22",
            "https://alphacephei.com/vosk/models/vosk-model-cn-0.22.zip"),

        ["Português/Portuguese Small (vosk-model-small-pt-0.3)"] = new("vosk-modelsmall-pt-0.3",
            "https://alphacephei.com/vosk/models/vosk-model-small-pt-0.3.zip"),
        ["Português/Portuguese Large (vosk-model-pt-fb-v0.1.1-20220516_2113)"] =
            new("vosk-model-pt-fb-v0.1.1-20220516_2113",
                "https://alphacephei.com/vosk/models/vosk-model-pt-fbv0.1.1-20220516_2113.zip"),

        ["Türkçe/Turkish Small (vosk-model-small-tr-0.3)"] = new("vosk-model-smalltr-0.3",
            "https://alphacephei.com/vosk/models/vosk-model-small-tr-0.3.zip"),
        ["Türkçe/Turkish Large (vosk-model-tr-0.3)"] = new("vosk-model-tr-0.3",
            "https://alphacephei.com/vosk/models/vosk-model-tr-0.3.zip"),

        ["日本語/Japanese Small (vosk-model-small-ja-0.22)"] = new("vosk-model-small-ja-0.22",
            "https://alphacephei.com/vosk/models/vosk-model-small-ja-0.22.zip"),
        ["日本語/Japanese Large (vosk-model-ja-0.22)"] = new("vosk-model-ja-0.22",
            "https://alphacephei.com/vosk/models/vosk-model-ja-0.22.zip"),

        ["Italiano/Italian Small (vosk-model-small-it-0.22)"] = new("vosk-modelsmall-it-0.22",
            "https://alphacephei.com/vosk/models/vosk-model-small-it-0.22.zip"),
        ["Italiano/Italian Large (vosk-model-it-0.22)"] = new("vosk-model-it-0.22",
            "https://alphacephei.com/vosk/models/vosk-model-it-0.22.zip"),

        ["Nederlands/Dutch Small (vosk-model-small-nl-0.22)"] = new("vosk-modelsmall-nl-0.22",
            "https://alphacephei.com/vosk/models/vosk-model-small-nl-0.22.zip"),
        ["Nederlands/Dutch Large (vosk-model-nl-spraakherkenning-0.6)"] = new("voskmodel-nl-spraakherkenning-0.6",
            "https://alphacephei.com/vosk/models/vosk-model-nlspraakherkenning-0.6.zip"),

        ["Українська/Ukrainian Small (vosk-model-small-uk-v3-small)"] = new("voskmodel-small-uk-v3-small",
            "https://alphacephei.com/vosk/models/vosk-model-small-uk-v3-small.zip"),
        ["Українська/Ukrainian Large (vosk-model-uk-v3)"] = new("vosk-model-uk-v3",
            "https://alphacephei.com/vosk/models/vosk-model-uk-v3.zip"),

        ["Svenska/Swedish Small (vosk-model-small-sv-rhasspy-0.15)"] = new("voskmodel-small-sv-rhasspy-0.15",
            "https://alphacephei.com/vosk/models/vosk-model-small-svrhasspy-0.15.zip"),
        ["Svenska/Swedish Large (vosk-model-sv-rhasspy-0.15)"] = new("vosk-model-svrhasspy-0.15",
            "https://alphacephei.com/vosk/models/vosk-model-svrhasspy-0.15.zip"),

        ["Polski/Polish Small (vosk-model-small-pl-0.22)"] = new("vosk-model-smallpl-0.22",
            "https://alphacephei.com/vosk/models/vosk-model-small-pl-0.22.zip"),
        ["Polski/Polish Large (vosk-model-pl-0.22)"] = new("vosk-model-pl-0.22",
            "https://alphacephei.com/vosk/models/vosk-model-pl-0.22.zip"),

        ["ۮहिन्दी/Hindi Small (vosk-model-small-hi-0.22)"] = new("vosk-model-smallhi-0.22",
            "https://alphacephei.com/vosk/models/vosk-model-small-hi-0.22.zip"),
        ["ۮहिन्दी/Hindi Large (vosk-model-hi-0.22)"] = new("vosk-model-hi-0.22",
            "https://alphacephei.com/vosk/models/vosk-model-hi-0.22.zip"),

        ["Čeština/Czech Small (vosk-model-small-cs-0.4-rhasspy)"] = new("vosk-modelsmall-cs-0.4-rhasspy",
            "https://alphacephei.com/vosk/models/vosk-model-small-cs-0.4-rhasspy.zip"),
        ["Čeština/Czech Large (vosk-model-cs-0.4-rhasspy)"] = new("vosk-modelcs-0.4-rhasspy",
            "https://alphacephei.com/vosk/models/vosk-model-cs-0.4-rhasspy.zip"),

        ["Tiếng Việt/Vietnamese Small (vosk-model-small-vn-0.4)"] = new("vosk-modelsmall-vn-0.4",
            "https://alphacephei.com/vosk/models/vosk-model-small-vn-0.4.zip"),
        ["Tiếng Việt/Vietnamese Large (vosk-model-vn-0.4)"] = new("vosk-modelvn-0.4",
            "https://alphacephei.com/vosk/models/vosk-model-vn-0.4.zip"),

        ["한국어/Korean Small (vosk-model-small-ko-0.22)"] = new("vosk-model-smallko-0.22",
            "https://alphacephei.com/vosk/models/vosk-model-small-ko-0.22.zip"),
        ["한국어/Korean Large (vosk-model-ko-0.22)"] = new("vosk-model-ko-0.22",
            "https://alphacephei.com/vosk/models/vosk-model-ko-0.22.zip"),
    };

    #endregion
}