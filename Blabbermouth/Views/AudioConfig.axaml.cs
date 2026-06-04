using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Blabbermouth.Core;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Structs;

namespace Blabbermouth.Views;

public partial class AudioConfig : UserControl
{
    public string? SelectedMicDeviceId => GetSelectedAudioDeviceId(MicInputComboBox);
    public string? SelectedSpeakersDeviceId => GetSelectedAudioDeviceId(SpeakersInputComboBox);
    
    public AudioConfig()
    {
        InitializeComponent();
    }

    private void PopulateAudioDevices(object? sender, RoutedEventArgs e)
    {
        MicInputComboBox.Items.Clear();
        SpeakersInputComboBox.Items.Clear();

        using var engine = new MiniAudioEngine();
        
        string lastMic = Settings.Get<string>("micDevice") ?? "";
        bool setToLastMic = false;
        foreach (DeviceInfo device in engine.CaptureDevices)
        {
            MicInputComboBox.Items.Add(new ComboBoxItem
            {
                Content = device.Name,
                Tag = device.Id,
            });
            if (!string.IsNullOrEmpty(lastMic) && device.Name == lastMic)
            {
                MicInputComboBox.SelectedIndex = MicInputComboBox.Items.Count - 1;
                setToLastMic = true;
            }
            if (device.IsDefault && !setToLastMic)
            {
                MicInputComboBox.SelectedIndex = MicInputComboBox.Items.Count - 1;
            }
        }

        if (OperatingSystem.IsWindows())
        {
            string lastSpeaker = Settings.Get<string>("speakerDevice") ?? "";
            bool setToLastSpeaker = false;
            foreach (DeviceInfo device in engine.PlaybackDevices)
            {
                SpeakersInputComboBox.Items.Add(new ComboBoxItem
                {
                    Content = device.Name,
                    Tag = device.Id,
                });
                if (!string.IsNullOrEmpty(lastSpeaker) && device.Name == lastSpeaker)
                {
                    SpeakersInputComboBox.SelectedIndex = SpeakersInputComboBox.Items.Count - 1;
                    setToLastSpeaker = true;
                }
                if (device.IsDefault && !setToLastSpeaker)
                {
                    SpeakersInputComboBox.SelectedIndex = SpeakersInputComboBox.Items.Count - 1;
                }
            }
        }
        else
        {
            SpeakersCheckBox.IsEnabled = false;
            SpeakersInputComboBox.IsEnabled = false;
            SpeakersInputComboBox.Items.Add(new ComboBoxItem
            {
                Content = "Only works on Windows :(",
                Tag = null,
            });
        }
        
        if (MicInputComboBox.Items.Count > 0 && MicInputComboBox.SelectedIndex < 0)
        {
            MicInputComboBox.SelectedIndex = 0;
        }

        if (SpeakersInputComboBox.Items.Count > 0 && SpeakersInputComboBox.SelectedIndex < 0)
        {
            SpeakersInputComboBox.SelectedIndex = 0;
        }

        SttManager.ListenToMic = (MicCheckBox.IsChecked = Settings.Get<bool>("useMic")) ?? false;
        SttManager.ListenToSpeakers = (SpeakersCheckBox.IsChecked = Settings.Get<bool>("useSpeaker")) ?? false;
    }

    private static string? GetSelectedAudioDeviceId(ComboBox comboBox)
    {
        return comboBox.SelectedItem switch
        {
            ComboBoxItem item => item.Tag?.ToString(),
            _ => null,
        };
    }

    private static string? GetSelectedAudioDeviceName(ComboBox comboBox)
    {
        return comboBox.SelectedItem switch
        {
            ComboBoxItem item => item.Name,
            _ => null,
        };
    }
    
    private void MicCheckBoxChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Set("useMic", SttManager.ListenToMic = MicCheckBox.IsChecked == true);
        if (SttManager.Enabled)
            SttManager.UpdateMicRecognizer();
    }

    private void SpeakersCheckBoxChanged(object? sender, RoutedEventArgs e)
    {
        Settings.Set("useSpeaker", SttManager.ListenToSpeakers = SpeakersCheckBox.IsChecked == true);
        if (SttManager.Enabled)
            SttManager.UpdateSpeakersRecognizer();
    }

    private void MicInputChanged(object? sender, SelectionChangedEventArgs e)
    {   
        Settings.Set("micDevice", GetSelectedAudioDeviceName(MicInputComboBox) ?? "");
        if (SttManager.Enabled)
            SttManager.UpdateMicRecognizer();
    }

    private void SpeakersInputChanged(object? sender, SelectionChangedEventArgs e)
    {   
        Settings.Set("speakerDevice", GetSelectedAudioDeviceName(SpeakersInputComboBox) ?? "");
        if (SttManager.Enabled)
            SttManager.UpdateSpeakersRecognizer();
    }
}