using System;
using System.IO;
using System.Linq;
using SherpaOnnx;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Enums;
using SoundFlow.Structs;

namespace Blabbermouth.SttProviders;

public sealed class SherpaOnnxProvider : ISpeechRecognizerProvider
{
    private OnlineRecognizer? _recognizer;
    private OnlineStream? _stream;
    private MiniAudioEngine? _engine;
    private AudioCaptureDevice? _capture;

    public event Action<string, bool>? Recognized;

    private readonly string _modelPath;

    public SherpaOnnxProvider(string modelPath)
    {
        _modelPath = modelPath;
    }

    public void Start(string deviceId, bool isLoopback)
    {
        Stop();
        string encoderPath = "";
        string decoderPath = "";
        string joinerPath = "";
        string tokensPath = "";
        foreach (string file in Directory.GetFiles(_modelPath))
        {
            if (!_modelPath.Contains("int8") && file.Contains("int8"))
            {
                Console.WriteLine($"Skipping int8 model file: {file}");
                continue;
            }
            if (file.Contains("encoder") && file.EndsWith(".onnx"))
                encoderPath = file;
            else if (file.Contains("decoder") && file.EndsWith(".onnx"))
                decoderPath = file;
            else if (file.Contains("joiner") && file.EndsWith(".onnx"))
                joinerPath = file;
            else if (file.Contains("tokens") && file.EndsWith(".txt"))
                tokensPath = file;
        }

        OnlineRecognizerConfig config = new()
        {
            ModelConfig = new()
            {
                Transducer = new()
                {
                    Encoder = encoderPath,
                    Decoder = decoderPath,
                    Joiner = joinerPath,
                },
                Provider = "cpu",
                Debug = 0,
                NumThreads = Environment.ProcessorCount / 4,
                Tokens = tokensPath,
            },
            FeatConfig = new()
            {
                SampleRate = 22050,
                FeatureDim = 80,
            },
            DecodingMethod = "modified_beam_search",
            MaxActivePaths = 4,
            EnableEndpoint = 1,
            Rule1MinTrailingSilence = 2.4f,
            Rule2MinTrailingSilence = 1.2f,
            Rule3MinUtteranceLength = 300,
        };

        _recognizer = new(config);
        _stream = _recognizer.CreateStream();

        _engine = new();

        AudioFormat format = new()
        {
            Format = SampleFormat.F32,
            Channels = 1,
            SampleRate = 22050,
        };

        _capture = isLoopback
            ? _engine.InitializeLoopbackDevice(format)
            : _engine.InitializeCaptureDevice(
                _engine.CaptureDevices.FirstOrDefault(d => d.Id.ToString() == deviceId),
                format);

        _capture.OnAudioProcessed += (samples, _) =>
        {
            if (_stream == null || _recognizer == null) return;

            _stream.AcceptWaveform(22050, samples.ToArray());

            while (_recognizer.IsReady(_stream))
            {
                _recognizer.Decode(_stream);
            }

            if (_recognizer.IsEndpoint(_stream))
            {
                OnlineRecognizerResult? result = _recognizer.GetResult(_stream);
                string text = result.Text;

                if (!string.IsNullOrWhiteSpace(text) && text != ".")
                {
                    Recognized?.Invoke(text.ToLowerInvariant(), false);
                }

                _recognizer.Reset(_stream);
            }
            else
            {
                OnlineRecognizerResult? result = _recognizer.GetResult(_stream);
                string text = result.Text;

                if (!string.IsNullOrWhiteSpace(text) && text != ".")
                {
                    Recognized?.Invoke(text.ToLowerInvariant(), true);
                }
            }
        };

        _capture.Start();
    }

    public void Stop()
    {
        if (_capture != null)
        {
            _capture.Stop();
            _capture.Dispose();
            _capture = null;
        }

        _engine?.Dispose();
        _engine = null;

        _stream?.Dispose();
        _stream = null;

        _recognizer?.Dispose();
        _recognizer = null;
    }

    public void Dispose() => Stop();
}