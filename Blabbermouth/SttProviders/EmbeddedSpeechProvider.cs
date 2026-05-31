using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Enums;
using SoundFlow.Structs;

namespace Blabbermouth.SttProviders;

public sealed class EmbeddedSpeechProvider : ISpeechRecognizerProvider
{
    private readonly EmbeddedSpeechConfig _speechConfig;
    private SpeechRecognizer? _recognizer;
    private PushAudioInputStream? _pushStream;
    private AudioConfig? _audioConfig;
    private MiniAudioEngine? _engine;
    private AudioCaptureDevice? _capture;
    
    public event Action<string>? Recognized;

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
                Recognized?.Invoke(words);
        };
        
        _ = _recognizer.StartContinuousRecognitionAsync();

        _engine = new();

        var format = new AudioFormat
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
        
        _capture.OnAudioProcessed += (samples, cap) =>
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