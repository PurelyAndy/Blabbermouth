using Avalonia.Media;

namespace Blabbermouth.Data;

public class TextSegment
{
    public readonly string Text;
    public readonly IBrush Foreground;
    public readonly IBrush Background;
    public readonly string? Tooltip;
    public int StartIndex;
    
    public TextSegment(string text, IBrush foreground, IBrush background, string? tooltip = null)
    {
        Text = text;
        Foreground = foreground;
        Background = background;
        Tooltip = tooltip;
    }
}