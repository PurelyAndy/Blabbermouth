using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Blabbermouth.Core;
using Blabbermouth.Data;
using Blabbermouth.Windows;

namespace Blabbermouth.Views.ModelSelection;

public sealed class EmbeddedModelSelector : ModelSelector
{
	private const SttKind Kind = SttKind.Embedded;
	
    public EmbeddedModelSelector()
    {
        Configure(
            header: "Select Model",
            subheader: "Higher versions work better",
            customPathHint: "...or if you happen to know the location of one:");
        ModelsLink.IsVisible = false;
    }

    public void UpdateModels()
    {
	    ModelSelect.ItemsSource = new List<string>([..SttManager.SpeechModels.Keys, ..Models.Keys]);
	    ModelSelect.SelectedIndex = Models.Count > 0 ? 0 : -1;
    }

    protected override async Task ContinueClicked()
    {
        if (!string.IsNullOrWhiteSpace(CustomModelPathValue))
        {
            if (await SetModelToCustom(CustomModelPathValue))
	            return;
            
            Settings.Set("lastModel", CustomModelPathValue);
            Settings.Set("lastModelWasCustom", true);

            MainWindow.CloseDialog();
            return;
        }
        
        if (SelectedModelName is not { } selectedModel) return;

        if (SttManager.SpeechModels.TryGetValue(selectedModel, out ModelInformation? existingModel))
        {
            SttManager.Model = existingModel;
            SttManager.Kind = Kind;
            SttManager.ResetRecognizers();
            
            Settings.Set("lastModel", selectedModel);

            MainWindow.CloseDialog();
            return;
        }

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
        
        string eula2 = EmbeddedSpeechLocator.GetEula(model.DirectoryPath)!;
        string version2 = EmbeddedSpeechLocator.GetVersion(model.DirectoryPath)!;
        SttManager.Model = new(eula2, model.DirectoryPath, version2);
        SttManager.Kind = Kind;
        SttManager.ResetRecognizers();
        
        Settings.Set("lastModel", selectedModel);
        ContinueButton.IsEnabled = true;
        MainWindow.CloseDialog();
    }

    public async Task<bool> SetModelToCustom(string path)
    {
	    string? eula = EmbeddedSpeechLocator.GetEula(path);
	    if (string.IsNullOrWhiteSpace(eula))
	    {
		    await MainWindow.ShowErrorAsync(
			    "This might not be a valid embedded speech model folder. " +
			    "If you're sure it is, please report this issue with information about where you got the model.",
			    "Could not find EULA");
		    return true;
	    }
	    string? version = EmbeddedSpeechLocator.GetVersion(path);
	    if (string.IsNullOrWhiteSpace(version))
	    {
		    await MainWindow.ShowErrorAsync(
			    "This might not be a valid embedded speech model folder. " +
			    "If you're sure it is, please report this issue with information about where you got the model.",
			    "Could not find version");
		    return true;
	    }
	    SttManager.Model = new(eula, path, version);
	    SttManager.Kind = Kind;
	    SttManager.ResetRecognizers();
	    return false;
    }

    #region Model download links

    public override Dictionary<string, DownloadableModel> Models => new()
    {
        ["English (download)"] = new("speechmodel.en-US.cpu.9.0.54436607",
            "https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech/0.16.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
        ["Dansk/Danish (download)"] = new("speechmodel.da-DK.cpu.1.0.40100365",
			"https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-da-dk/0.5.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
        ["Deutsch/German (download)"] = new("speechmodel.de-DE.cpu.2.0.40100365",
		    "https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-de-de/0.5.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
		["Australian English (download)"] = new("speechmodel.en-AU.cpu.9.0.54444427",
			"https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-en-au/0.5.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
		["Canadian English (download)"] = new("speechmodel.en-CA.cpu.9.0.54444427",
			"https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-en-ca/0.5.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
		["British English (download)"] = new("speechmodel.en-GB.cpu.9.0.54444427",
			"https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-en-gb/0.5.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
		["Irish English (download)"] = new("speechmodel.en-IE.cpu.9.0.54444427",
			"https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-en-ie/0.5.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
		["Indian English (download)"] = new("speechmodel.en-IN.cpu.2.0.40100365",
			"https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-en-in/0.5.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
		["New Zealand English (download)"] = new("speechmodel.en-NZ.cpu.9.0.54444427",
			"https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-en-nz/0.5.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
		["Español/Spanish (download)"] = new("speechmodel.es-ES.cpu.2.1.40100365",
			"https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-es-es/0.5.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
		["Español de México/Mexican Spanish (download)"] = new("speechmodel.es-MX.cpu.1.1.40100365",
			"https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-es-mx/0.5.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
		["Français/Français (download)"] = new("speechmodel.fr-FR.cpu.2.1.41371463",
			"https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-fr-fr/0.5.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
		["Français canadien/Canadian French (download)"] = new("speechmodel.fr-CA.cpu.1.1.40100365",
			"https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-fr-ca/0.5.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
		["Hindi (download)"] = new("speechmodel.hi-IN.cpu.2.0.32339472",
			"https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-hi-in/0.5.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
		["Italiano/Italian (download)"] = new("speechmodel.it-IT.cpu.2.1.40100365",
			"https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-it-it/0.5.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
		["日本語/Japanese (download)"] = new("speechmodel.ja-JP.cpu.2.1.40100365",
			"https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-ja-jp/0.5.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
		["한국어/Korean (download)"] = new("speechmodel.ko-KR.cpu.2.1.40100365",
			"https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-ko-kr/0.5.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
		["Nederlands/Dutch (download)"] = new("speechmodel.nl-NL.cpu.1.0.32339472",
			"https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-nl-nl/0.5.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
		["Português/Portuguese (download)"] = new("speechmodel.pt-PT.cpu.1.0.40100365",
			"https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-pt-pt/0.5.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
		["Português do Brasil/Brazilian Portuguese (download)"] = new("speechmodel.pt-BR.cpu.2.1.40100365",
			"https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-pt-br/0.5.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
		["русский/Russian (download)"] = new("speechmodel.ru-RU.cpu.2.0.32339472",
			"https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-ru-ru/0.5.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
		["Svenska/Swedish (download)"] = new("speechmodel.sv-SE.cpu.1.0.32339472",
			"https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-sv-se/0.5.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
		["Türkçe/Turkish (download)"] = new("speechmodel.tr-TR.cpu.1.0.32339472",
			"https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-tr-tr/0.5.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
		["中文/Chinese (download)"] = new("speechmodel.zh-CN.cpu.3.1.40100365",
			"https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-zh-cn/0.5.1/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
		["中文（香港）/Chinese (Hong Kong) (download)"] = new("speechmodel.zh-HK.cpu.2.1.40100365",
			"https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-zh-hk/0.5.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage"),
		["中文（台湾）/Chinese (Taiwan) (download)"] = new("speechmodel.zh-TW.cpu.2.1.40100365",
			"https://ms-vscode.gallery.vsassets.io/_apis/public/gallery/publisher/ms-vscode/extension/vscode-speech-language-pack-zh-tw/0.5.0/assetbyname/Microsoft.VisualStudio.Services.VSIXPackage")
    };
    #endregion
}
