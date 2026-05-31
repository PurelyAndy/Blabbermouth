using System;

namespace Blabbermouth.SttProviders;

public interface ISpeechRecognizerProvider : IDisposable
{
    event Action<string>? Recognized;
    void Start(string deviceId, bool isLoopback);
    void Stop();
}

