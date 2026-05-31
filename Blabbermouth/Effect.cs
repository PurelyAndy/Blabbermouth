using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Blabbermouth;

public enum Effect
{
    Shock,
    Vibration,
    Both,
}

public class EffectEmojiConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Effect effect)
        {
            return effect switch
            {
                Effect.Shock => "⚡",
                Effect.Vibration => "📳",
                Effect.Both => "🌩️",
                _ => null,
            };
        }
        return null;
    }
    
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}