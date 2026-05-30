using System;
using System.Globalization;

namespace Blabbermouth;

public class PhraseEntry
{
    public string Phrase { get; set; } = "";
    public int Intensity { get; set; } = 1;
    public double Seconds { get; set; } = 0.3;
    public Effect Effect { get; set; } = Effect.Shock;
    public Activation Activation { get; set; } = IsWindows ? Activation.Both : Activation.Microphone;
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