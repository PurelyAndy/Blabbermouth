using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Blabbermouth;

public partial class PhraseEntry : ObservableObject
{
    [ObservableProperty] public partial string Phrase { get; set; } = "trigger text";
    [ObservableProperty] public partial int Intensity { get; set; } = 1;
    [ObservableProperty] public partial double Seconds { get; set; } = 0.3;
    [ObservableProperty] public partial Effect Effect { get; set; } = Effect.Shock;
    [ObservableProperty] public partial Activation Activation { get; set; } = IsWindows ? Activation.Both : Activation.Microphone;
    public static bool IsWindows => OperatingSystem.IsWindows();

    public string GetActionString()
    {
        return $"{Effect switch
        {
            Effect.Shock => "Shocked",
            Effect.Vibration => "Vibrated",
            Effect.Both => "Shocked and vibrated",
            _ => "???",
        }} for {Seconds} second{(Seconds.ToString(CultureInfo.InvariantCulture) == "1" ? "" : "s")} at intensity {Intensity}";
    }
}