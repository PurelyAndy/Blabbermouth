using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Blabbermouth.Data;

public enum OperationKind
{
    Shock,
    Vibration,
    Sound,
    Application,
    Wait,
}

public class OperationKindEmojiConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is OperationKind effect)
        {
            return effect switch
            {
                OperationKind.Shock => "⚡",
                OperationKind.Vibration => "📳",
                OperationKind.Sound => "🔊",
                OperationKind.Application => "🖥️",
                OperationKind.Wait => "⏳",
                _ => null,
            };
        }
        return null;
    }
    
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}