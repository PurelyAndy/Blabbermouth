namespace Blabbermouth.Util;

public static class Extensions
{
    public static string ToPastTense(this string s)
    {
        if (s == "Do nothing.") return "Did nothing.";
        return s
            .Replace("shock", "shocked")
            .Replace("Shock", "Shocked")
            .Replace("vibrate", "vibrated")
            .Replace("Vibrate", "Vibrated")
            .Replace("play sound", "played sound")
            .Replace("Play sound", "Played sound")
            .Replace("launch application", "launched application")
            .Replace("Launch application", "Launched application")
            .Replace("wait", "waited")
            .Replace("Wait", "Waited");
    }
}