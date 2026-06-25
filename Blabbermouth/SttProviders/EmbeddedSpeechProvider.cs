using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Blabbermouth.Core;
using Blabbermouth.Data;
using Blabbermouth.Util;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Enums;
using SoundFlow.Structs;

namespace Blabbermouth.SttProviders;

public sealed partial class EmbeddedSpeechProvider : ISpeechRecognizerProvider
{
    private readonly EmbeddedSpeechConfig _speechConfig;
    private SpeechRecognizer? _recognizer;
    private PushAudioInputStream? _pushStream;
    private AudioConfig? _audioConfig;
    private MiniAudioEngine? _engine;
    private AudioCaptureDevice? _capture;

    public event Action<string, bool>? Recognized;

    public EmbeddedSpeechProvider(ModelInformation model)
    {
        _speechConfig = EmbeddedSpeechConfig.FromPath(model.Path);
        if (_speechConfig == null)
        {
            throw new InvalidOperationException("Failed to load embedded speech model.");
        }
        _speechConfig.SetSpeechRecognitionModel(model.Version, model.Eula);
        _speechConfig.SetProfanity(ProfanityOption.Raw);
        _speechConfig.SpeechRecognitionOutputFormat = OutputFormat.Detailed;
    }

    public void Start(string deviceId, bool isLoopback)
    {
        Stop();

        _pushStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1));
        _audioConfig = AudioConfig.FromStreamInput(_pushStream);
        _recognizer = new(_speechConfig, _audioConfig);

        _recognizer.Recognized += (_, e) =>
        {
            if (e.Result.Reason != ResultReason.RecognizedSpeech) return;

            IEnumerable<LexicalResult> results = Best(e.Result);
            string? words = results.FirstOrDefault()?.Lexical?.ToLower();
            if (!string.IsNullOrWhiteSpace(words))
                Recognized?.Invoke(words, false);
        };
        _recognizer.Recognizing += (_, e) =>
        {
            if (!SttManager.DetectBeforeDoneTalking) return;
            if (e.Result.Reason != ResultReason.RecognizingSpeech) return;

            if (!string.IsNullOrWhiteSpace(e.Result.Text))
                Recognized?.Invoke(RawLexical(e.Result.Text), true);
        };

        _ = _recognizer.StartContinuousRecognitionAsync();

        _engine = new();

        AudioFormat format = new()
        {
            Format = SampleFormat.F32,
            Channels = 1,
            SampleRate = 16000,
        };

        if (isLoopback)
        {
            _capture = _engine.InitializeLoopbackDevice(format);
        }
        else
        {
            DeviceInfo device = _engine.CaptureDevices.FirstOrDefault(d => d.Id.ToString() == deviceId);
            _capture = _engine.InitializeCaptureDevice(device, format);
        }

        _capture.OnAudioProcessed += (samples, _) =>
        {
            byte[] bytes = new byte[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                short val = (short)(Math.Clamp(samples[i], -1f, 1f) * short.MaxValue);
                bytes[i * 2] = (byte)(val & 0xFF);
                bytes[i * 2 + 1] = (byte)(val >> 8);
            }
            _pushStream?.Write(bytes);
        };

        _capture.Start();
    }

    public void Stop()
    {
        _capture?.Stop();
        _capture?.Dispose();
        _capture = null;

        _engine?.Dispose();
        _engine = null;

        _audioConfig?.Dispose();
        _audioConfig = null;

        _pushStream?.Dispose();
        _pushStream = null;

        _recognizer?.Dispose();
        _recognizer = null;
    }

    public void Dispose()
    {
        Stop();
    }

    private static List<LexicalResult> Best(SpeechRecognitionResult result)
    {
        string? json =
            result.Properties.GetProperty(
                PropertyId.SpeechServiceResponse_JsonResult);

        return JsonSerializer.Deserialize(
                       json,
                       SpeechJsonContext.Default
                           .DetailedSpeechRecognitionResultCollection)
                   ?.NBest
               ?? [];
    }

    private static readonly HashSet<string> KnownContractions =
    [
        "it's", "that's", "what's", "who's", "he's", "she's", "where's", "there's", "here's", "how's",
        "i'll", "you'll", "he'll", "she'll", "it'll", "we'll", "they'll", "that'll", "what'll", "who'll",
        "i'd", "you'd", "he'd", "she'd", "it'd", "we'd", "they'd", "that'd", "what'd", "who'd",
        "i've", "you've", "we've", "they've",
        "could've", "should've", "would've", "might've", "must've",
        "can't", "cannot", "won't", "don't", "doesn't", "didn't", "isn't", "aren't", "wasn't", "weren't",
        "haven't", "hasn't", "hadn't", "wouldn't", "shouldn't", "couldn't", "mustn't", "needn't",
        "let's",
    ];
    private static string RawLexical(string text)
    {
        // the "lexical" field in the detailed result contains the raw recognized text without punctuation and certain
        // formatting, but it's not available for partial results, so we have to clean it up ourselves
        string initial = text.ToLower().Replace(".", "").Replace(",", "").Replace("!", "").Replace("?", "");
        initial = NumbersRegex().Replace(initial, m => NumbersToWords.Perform(m.Value));
        initial = ContractionsRegex().Replace(initial, m =>
        {
            string contraction = m.Value.ToLower();
            if (KnownContractions.Contains(contraction))
                return contraction;
            return m.Groups[1].Value + " " + m.Groups[2].Value;
        });
        return initial;
    }

    [GeneratedRegex(@"-?((?:\d{4})|(?:\d{1,3}(?:,\d{3})+)|\d+)?(?:\.\d+)?")]
    private static partial Regex NumbersRegex();

    [GeneratedRegex(@"(\w+)('\w+)")]
    private static partial Regex ContractionsRegex();
}

public sealed class DetailedSpeechRecognitionResultCollection
{
    public List<LexicalResult>? NBest { get; set; }
}
public sealed class LexicalResult
{
    public string? Lexical { get; set; }
}
[JsonSerializable(typeof(LexicalResult))]
[JsonSerializable(typeof(DetailedSpeechRecognitionResultCollection))]
internal partial class SpeechJsonContext : JsonSerializerContext;