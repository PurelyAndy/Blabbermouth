using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Blabbermouth;

[Flags]
public enum Activation
{
    Microphone = 1,
    Speakers = 2,
    Both = 3,
}

public class ActivationEmojiConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Activation activation)
        {
            return activation switch
            {
                Activation.Microphone => "🎤🔇",
                Activation.Speakers => "🚫🔊",
                Activation.Both => "🎤🔊",
                _ => null,
            };
        }
        return null;
    }
    
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}