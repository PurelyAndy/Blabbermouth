using System;
using System.Linq;
using System.Text.Json;
using Blabbermouth.Core;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Enums;
using SoundFlow.Structs;
using Vosk;

namespace Blabbermouth.SttProviders;

public sealed class VoskProvider : ISpeechRecognizerProvider
{
    private Model? _model;
    private VoskRecognizer? _recognizer;
    private MiniAudioEngine? _engine;
    private AudioCaptureDevice? _capture;

    public event Action<string, bool>? Recognized;

    private readonly string _modelPath;

    public VoskProvider(string modelPath)
    {
        _modelPath = modelPath;
    }

    public void Start(string deviceId, bool isLoopback)
    {
        Stop();

        _model = new(_modelPath);
        _recognizer = new(_model, 16000);

        _engine = new();

        AudioFormat format = new()
        {
            Format = SampleFormat.F32,
            Channels = 1,
            SampleRate = 16000,
        };

        _capture = isLoopback
            ? _engine.InitializeLoopbackDevice(format)
            : _engine.InitializeCaptureDevice(
                _engine.CaptureDevices.FirstOrDefault(d => d.Id.ToString() == deviceId),
                format);

        _capture.OnAudioProcessed += (samples, _) =>
        {
            if (_recognizer == null) return;

            byte[] pcm16 = new byte[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                short val = (short)(Math.Clamp(samples[i], -1f, 1f) * short.MaxValue);
                pcm16[i * 2] = (byte)(val & 0xFF);
                pcm16[i * 2 + 1] = (byte)(val >> 8);
            }

            if (_recognizer.AcceptWaveform(pcm16, pcm16.Length))
            {
                string json = _recognizer.Result();
                string? text = TryGetText(json, "text");
                if (!string.IsNullOrWhiteSpace(text))
                {
                    Recognized?.Invoke(text.ToLowerInvariant(), false);
                }
            }
            else if (SttManager.DetectBeforeDoneTalking)
            {
                string json = _recognizer.PartialResult();
                string? text = TryGetText(json, "partial");
                if (SttManager.DetectBeforeDoneTalking && !string.IsNullOrWhiteSpace(text))
                {
                    Recognized?.Invoke(text.ToLowerInvariant(), true);
                }
            }
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

        _recognizer?.Dispose();
        _recognizer = null;

        _model?.Dispose();
        _model = null;
    }

    public void Dispose() => Stop();

    private static string? TryGetText(string json, string propertyName)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty(propertyName, out JsonElement textEl)
            ? textEl.GetString()
            : null;
    }
}