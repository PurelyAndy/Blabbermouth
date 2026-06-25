namespace Blabbermouth.Util;

public static class Extensions
{
    public static string ToPastTense(this string s)
    {
        if (s == "Do nothing.") return "Did nothing.";
        return s
            .Replace("shock for", "shocked for")
            .Replace("Shock for", "Shocked for")
            .Replace("vibrate for", "vibrated for")
            .Replace("Vibrate for", "Vibrated for")
            .Replace("beep for", "beeped for")
            .Replace("Beep for", "Beeped for")
            .Replace("play sound", "played sound")
            .Replace("Play sound", "Played sound")
            .Replace("launch application", "launched application")
            .Replace("Launch application", "Launched application")
            .Replace("wait", "waited")
            .Replace("Wait", "Waited");
    }
    
    public static int Mod(this int x, int m)
    {
        return (x % m + m) % m;
    }
}