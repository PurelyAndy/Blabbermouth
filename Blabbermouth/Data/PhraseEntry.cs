using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Blabbermouth.Data;

public partial class PhraseEntry : ObservableObject
{
    [ObservableProperty] public partial string Phrase { get; set; } = "trigger text";
    [ObservableProperty] public partial OperationSequence Operations { get; set; } = new();
    [ObservableProperty] public partial Activation Activation { get; set; } = IsWindows ? Activation.Both : Activation.Microphone;
    public static bool IsWindows => OperatingSystem.IsWindows();
}