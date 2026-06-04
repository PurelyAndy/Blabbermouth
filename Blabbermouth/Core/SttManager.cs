using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using Blabbermouth.Data;
using Blabbermouth.SttProviders;
using Blabbermouth.Util;
using Blabbermouth.Windows;

namespace Blabbermouth.Core;

public static class SttManager
{
    public static bool ListenToMic;
    public static bool ListenToSpeakers;
    public static readonly Dictionary<string, ModelInformation> SpeechModels = [];
    public static ModelInformation Model = null!;
    public static bool Enabled;
    private static ISpeechRecognizerProvider? _micRecognizer;
    private static ISpeechRecognizerProvider? _speakersRecognizer;
    public static SttKind Kind
    {
        get;
        set => Settings.Set("mode", field = value);
    }

    static SttManager()
    {
        Kind = Settings.Get<SttKind>("mode");
    }

    private static ISpeechRecognizerProvider? CreateProvider()
    {
        return Kind switch
        {
            SttKind.Embedded => new EmbeddedSpeechProvider(Model),
            SttKind.Vosk => new VoskProvider(Model.Path),
            SttKind.SherpaOnnx => new SherpaOnnxProvider(Model.Path),
            _ => null,
        };
    }

    public static void UpdateMicRecognizer()
    {
        _micRecognizer?.Stop();
        if (!ListenToMic) return;

        string? deviceId = MainWindow.I.AudioConfig.SelectedMicDeviceId;
        if (string.IsNullOrWhiteSpace(deviceId)) return;

        if (_micRecognizer == null)
        {
            _micRecognizer = CreateProvider();
            if (_micRecognizer != null)
            {
                _micRecognizer.Recognized += async words =>
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        await ProcessRecognizedSpeech(words, Activation.Microphone, Brushes.LightGreen);
                    });
                };
            }
        }
        _micRecognizer?.Start(deviceId, isLoopback: false);
    }

    public static void UpdateSpeakersRecognizer()
    {
        _speakersRecognizer?.Stop();
        if (!ListenToSpeakers || !OperatingSystem.IsWindows()) return;

        string? deviceId = MainWindow.I.AudioConfig.SelectedSpeakersDeviceId;
        if (string.IsNullOrWhiteSpace(deviceId)) return;

        if (_speakersRecognizer == null)
        {
            _speakersRecognizer = CreateProvider();
            if (_speakersRecognizer != null)
            {
                _speakersRecognizer.Recognized += async words =>
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        await ProcessRecognizedSpeech(words, Activation.Speakers, Brushes.LightSkyBlue);
                    });
                };
            }
        }

        _speakersRecognizer?.Start(deviceId, isLoopback: true);
    }

    private static async Task ProcessRecognizedSpeech(string words, Activation activation, ISolidColorBrush defaultBrush)
    {
        PhraseEntry? foundPhrase = MainWindow.I.PhraseList.Phrases.FirstOrDefault(p =>
            (p.Activation & activation) != 0
            && (   words.Contains($" {p.Phrase} ", StringComparison.CurrentCultureIgnoreCase)
                || words.StartsWith($"{p.Phrase} ", StringComparison.CurrentCultureIgnoreCase)
                || words.EndsWith($" {p.Phrase}", StringComparison.CurrentCultureIgnoreCase)
                || words == p.Phrase
                ));
        string icon = activation == Activation.Microphone ? "🎤" : "🔊";
        string output = $"{icon} {words}\n";

        var addedSegments = new List<TextSegment>();
        if (foundPhrase == null)
        {
            addedSegments.Add(new(output, defaultBrush, Brushes.Transparent));
        }
        else
        {
            SolidColorBrush background = new(defaultBrush.Color, 0.2);
            
            int index = output.IndexOf($" {foundPhrase.Phrase} ", StringComparison.CurrentCultureIgnoreCase);
            bool atEnd = index < 0;
            if (atEnd)
            {
                // no need to check the beginning because `output` starts with the icon and a space
                index = output.LastIndexOf($" {foundPhrase.Phrase}", StringComparison.CurrentCultureIgnoreCase);
            }
            
            addedSegments.Add(new(output[..(index + 1)], defaultBrush, Brushes.Transparent));
            addedSegments.Add(new(output[(index + 1)..(index + 1 + foundPhrase.Phrase.Length)], Brushes.OrangeRed, background, foundPhrase.Operations.ToString().ToPastTense()));
            addedSegments.Add(new(output[(index + 1 + foundPhrase.Phrase.Length)..], defaultBrush, Brushes.Transparent));
            await foundPhrase.Operations.Perform();
        }

        MainWindow.I.Monitor.AddSegments(addedSegments);
    }

    public static async Task Operate(int intensity, double seconds, bool shock)
    {
        int ms = (int)(seconds * 1000);
        if (MainWindow.I.ShockerConfig.UsingSerial)
        {
            await PiShock.SerialOperate(intensity, ms, shock);
        }
        else
        {
            string response = await PiShock.Operate(intensity, ms, shock);
            Debug.WriteLine($"PiShock response: {response}");
        }
    }

    private static void DisposeRecognizer(ref ISpeechRecognizerProvider? recognizer)
    {
        recognizer?.Dispose();
        recognizer = null;
    }


    public static void DisposeRecognizers()
    {
        DisposeRecognizer(ref _micRecognizer);
        DisposeRecognizer(ref _speakersRecognizer);
    }

    public static void UpdateRecognizers()
    {
        UpdateMicRecognizer();
        UpdateSpeakersRecognizer();
    }

    public static void ResetRecognizers()
    {
        DisposeRecognizers();
        UpdateRecognizers();
    }
}