using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Codecs.FFMpeg;
using SoundFlow.Components;
using SoundFlow.Interfaces;
using SoundFlow.Providers;
using SoundFlow.Structs;

namespace Blabbermouth.Core;

public static class PlaybackManager
{
    private static readonly MiniAudioEngine Engine = new();
    private static readonly Dictionary<AudioFormat, AudioPlaybackDevice> PlaybackDevices = [];
    private static readonly ConcurrentBag<Task> PlaybackTasks = [];

    static PlaybackManager()
    {
        Engine.RegisterCodecFactory(new FFmpegCodecFactory());
        Engine.UpdateAudioDevicesInfo();
    }

    public static async Task PlayAudio(string audioFilePath)
    {
        await using FileStream fs = new(audioFilePath, FileMode.Open, FileAccess.Read);
        using ISoundDecoder decoder = Engine.CreateDecoder(fs, out AudioFormat format);
        float[] buffer = new float[decoder.Length];
        decoder.Decode(buffer);

        using RawDataProvider dataProvider = new(buffer, format.SampleRate);
        SoundPlayer player = new(Engine, format, dataProvider);

        DeviceInfo defaultDeviceInfo = Engine.PlaybackDevices.FirstOrDefault(x => x.IsDefault);
        using AudioPlaybackDevice playbackDevice = Engine.InitializePlaybackDevice(defaultDeviceInfo, format);

        playbackDevice.MasterMixer.AddComponent(player);
        playbackDevice.Start();
        player.Play();
        await Task.Delay((int)(player.Duration * 1000) + 50);
        playbackDevice.Stop();
    }
}