using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Channels;
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
    public static bool DetectBeforeDoneTalking;
    public static bool AllowMultiplePhrases;
    public static bool AllowMultipleOfSamePhrase;
    private static ISpeechRecognizerProvider? _micRecognizer;
    private static ISpeechRecognizerProvider? _speakersRecognizer;
    private static readonly Channel<OperationSequence> OperationQueue = Channel.CreateUnbounded<OperationSequence>();

    public static SttKind Kind
    {
        get;
        set => Settings.Set("mode", field = value);
    }

    static SttManager()
    {
        Kind = Settings.Get<SttKind>("mode");
        _ = ProcessOperationQueueAsync();
    }

    private static async Task ProcessOperationQueueAsync()
    {
        await foreach (OperationSequence operations in OperationQueue.Reader.ReadAllAsync())
        {
            await operations.Perform();
        }
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
        if (!ListenToMic || !Enabled) return;

        string? deviceId = MainWindow.I.AudioConfig.SelectedMicDeviceId;
        if (string.IsNullOrWhiteSpace(deviceId)) return;

        if (_micRecognizer == null)
        {
            _micRecognizer = CreateProvider();
            if (_micRecognizer != null)
            {
                _micRecognizer.Recognized += (words, partial) =>
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        ProcessRecognizedSpeech(words, Activation.Microphone, partial, Brushes.LightGreen);
                    });
                };
            }
        }
        _micRecognizer?.Start(deviceId, isLoopback: false);
    }

    public static void UpdateSpeakersRecognizer()
    {
        _speakersRecognizer?.Stop();
        if (!ListenToSpeakers || !Enabled || !OperatingSystem.IsWindows()) return;

        string? deviceId = MainWindow.I.AudioConfig.SelectedSpeakersDeviceId;
        if (string.IsNullOrWhiteSpace(deviceId)) return;

        if (_speakersRecognizer == null)
        {
            _speakersRecognizer = CreateProvider();
            if (_speakersRecognizer != null)
            {
                _speakersRecognizer.Recognized += (words, partial) =>
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        ProcessRecognizedSpeech(words, Activation.Speakers, partial, Brushes.LightSkyBlue);
                    });
                };
            }
        }

        _speakersRecognizer?.Start(deviceId, isLoopback: true);
    }

    private static readonly Dictionary<PhraseEntry, int> SeenPhrasesMic = new();
    private static readonly Dictionary<PhraseEntry, int> SeenPhrasesSpeakers = new();
    private static void ProcessRecognizedSpeech(string words, Activation activation, bool partial, ISolidColorBrush defaultBrush)
    {
        List<PhraseEntry> phrasesToPerform = [];
        List<PhraseEntry> foundPhrases = [];
        if (AllowMultiplePhrases)
        {
            foundPhrases = MainWindow.I.PhraseList.Phrases.Where(PhraseFits).ToList();
        }
        else
        {
            PhraseEntry? foundPhrase = MainWindow.I.PhraseList.Phrases.FirstOrDefault(PhraseFits);
            if (foundPhrase != null)
            {
                foundPhrases.Add(foundPhrase);
            }
        }

        string icon = activation == Activation.Microphone ? "🎤" : "🔊";
        string output = $"{icon} {words}\n";

        List<TextSegment> addedSegments = [];
        if (foundPhrases.Count == 0)
        {
            addedSegments.Add(new(output, defaultBrush, Brushes.Transparent));
        }
        else
        {
            SolidColorBrush background = new(defaultBrush.Color, 0.2);
            List<(int Start, int Length, PhraseEntry Phrase)> matches = [];

            foreach (PhraseEntry p in foundPhrases)
            {
                string midTarget = $" {p.Phrase} ";
                int index = output.IndexOf(midTarget, StringComparison.CurrentCultureIgnoreCase);
                while (index >= 0)
                {
                    matches.Add((index + 1, p.Phrase.Length, p));
                    index = output.IndexOf(midTarget, index + 1, StringComparison.CurrentCultureIgnoreCase);
                }

                string endTarget = $" {p.Phrase}\n";
                if (output.EndsWith(endTarget, StringComparison.CurrentCultureIgnoreCase))
                {
                    int endIndex = output.Length - endTarget.Length;
                    matches.Add((endIndex + 1, p.Phrase.Length, p));
                }
            }

            var orderedMatches = matches
                .OrderBy(m => m.Start)
                .ThenByDescending(m => m.Length)
                .ToList();

            int currentIdx = 0;
            foreach ((int start, int length, PhraseEntry phrase) in orderedMatches)
            {
                if (start < currentIdx)
                {
                    continue;
                }

                if (start > currentIdx)
                {
                    addedSegments.Add(new(output[currentIdx..start], defaultBrush, Brushes.Transparent));
                }

                string phraseText = output[start..(start + length)];
                addedSegments.Add(new(phraseText, Brushes.OrangeRed, background, phrase.Operations.ToString().ToPastTense()));

                currentIdx = start + length;

                if (AllowMultipleOfSamePhrase || !phrasesToPerform.Contains(phrase))
                    phrasesToPerform.Add(phrase);
            }

            if (currentIdx < output.Length)
            {
                addedSegments.Add(new(output[currentIdx..], defaultBrush, Brushes.Transparent));
            }
        }

        MainWindow.I.Monitor.AddSegments(activation, partial, addedSegments);

        Dictionary<PhraseEntry, int> seenPhrases =
            activation == Activation.Microphone
                ? SeenPhrasesMic
                : SeenPhrasesSpeakers;

        List<OperationSequence> finalOperations = [];

        foreach (PhraseEntry phrase in phrasesToPerform)
        {
            seenPhrases.TryGetValue(phrase, out int count);
            if (count > 0)
            {
                seenPhrases[phrase]--;
                continue;
            }
            finalOperations.Add(phrase.Operations);
        }

        seenPhrases.Clear();
        if (partial)
        {
            foreach (PhraseEntry phrase in phrasesToPerform)
            {
                if (!seenPhrases.TryAdd(phrase, 1))
                {
                    seenPhrases[phrase]++;
                }
            }
        }

        foreach (OperationSequence operations in finalOperations)
        {
            OperationQueue.Writer.TryWrite(operations);
        }

        return;

        bool PhraseFits(PhraseEntry p) =>
            (p.Activation & activation) != 0
            && (words.Contains($" {p.Phrase} ", StringComparison.CurrentCultureIgnoreCase)
             || words.StartsWith($"{p.Phrase} ", StringComparison.CurrentCultureIgnoreCase)
             || words.EndsWith($" {p.Phrase}", StringComparison.CurrentCultureIgnoreCase)
             || words == p.Phrase);
    }

    public static async Task Operate(int intensity, double seconds, ShockerAction op)
    {
        int ms = (int)(seconds * 1000);
        if (MainWindow.I.ShockerConfig.UsingSerial)
        {
            await PiShock.SerialOperate(intensity, ms, op);
        }
        else
        {
            string response = await PiShock.Operate(intensity, ms, op);
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