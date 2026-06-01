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

        foreach (DeviceInfo device in engine.CaptureDevices)
        {
            MicInputComboBox.Items.Add(new ComboBoxItem
            {
                Content = device.Name,
                Tag = device.Id,
            });
            if (device.IsDefault)
            {
                MicInputComboBox.SelectedIndex = MicInputComboBox.Items.Count - 1;
            }
        }

        if (OperatingSystem.IsWindows())
        {
            foreach (DeviceInfo device in engine.PlaybackDevices)
            {
                SpeakersInputComboBox.Items.Add(new ComboBoxItem
                {
                    Content = device.Name,
                    Tag = device.Id,
                });
                if (device.IsDefault)
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
    }

    private static string? GetSelectedAudioDeviceId(ComboBox comboBox)
    {
        return comboBox.SelectedItem switch
        {
            ComboBoxItem item => item.Tag?.ToString(),
            _ => null,
        };
    }
    
    private void MicCheckBoxChanged(object? sender, RoutedEventArgs e)
    {
        SttManager.ListenToMic = MicCheckBox.IsChecked == true;
        if (SttManager.Enabled)
            SttManager.UpdateMicRecognizer();
    }

    private void SpeakersCheckBoxChanged(object? sender, RoutedEventArgs e)
    {
        SttManager.ListenToSpeakers = SpeakersCheckBox.IsChecked == true;
        if (SttManager.Enabled)
            SttManager.UpdateSpeakersRecognizer();
    }

    private void MicInputChanged(object? sender, SelectionChangedEventArgs e)
    {   
        if (SttManager.Enabled)
            SttManager.UpdateMicRecognizer();
    }

    private void SpeakersInputChanged(object? sender, SelectionChangedEventArgs e)
    {   
        if (SttManager.Enabled)
            SttManager.UpdateSpeakersRecognizer();
    }
}