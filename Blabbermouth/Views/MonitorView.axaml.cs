using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;

namespace Blabbermouth.Views;

public partial class MonitorView : UserControl
{
    private readonly List<TextSegment> _monitorTextSegments = [];

    public MonitorView()
    {
        InitializeComponent();
    }

    public void AddSegments(IEnumerable<TextSegment> segments)
    {
        _monitorTextSegments.AddRange(segments);
        ClampText();
        ApplyStyles();
        OutputScroller.ScrollToEnd();
    }

    private void ClampText()
    {
        const int maxLines = 50;

        int lines = 0;
        int i = _monitorTextSegments.Count - 1;
        for (; i >= 0; i--)
        {
            if (_monitorTextSegments[i].Text.Contains('\n'))
            {
                lines++;
            }
            if (lines > maxLines)
            {
                i++;
                break;
            }
        }
        if (i > 0)
        {
            _monitorTextSegments.RemoveRange(0, i);
        }
    }

    private void ApplyStyles()
    {
        if (_monitorTextSegments.Count == 0)
            return;

        MonitorBlock.Text = null;
        MonitorBlock.Inlines?.Clear();

        foreach (TextSegment segment in _monitorTextSegments.Where(segment => !string.IsNullOrEmpty(segment.Text)))
        {
            if (!string.IsNullOrEmpty(segment.Tooltip))
            {
                const int borderWidth = 1;
                Border border = new()
                {
                    Background = segment.Background,
                    BorderBrush = segment.Foreground,
                    BorderThickness = new(borderWidth),
                    Margin = new(-borderWidth, -borderWidth, -borderWidth, -3-borderWidth),
                    Padding = new(0, 0, 0, -2),
                    CornerRadius = new(2),
                    Cursor = new(StandardCursorType.Hand),
                    Child = new SelectableTextBlock
                    {
                        Text = segment.Text,
                        Foreground = segment.Foreground,
                    },
                };

                ToolTip.SetTip(border, segment.Tooltip);

                MonitorBlock.Inlines?.Add(new InlineUIContainer(border));
            }
            else
            {
                var run = new Run(segment.Text)
                {
                    Foreground = segment.Foreground,
                    Background = segment.Background,
                };
                MonitorBlock.Inlines?.Add(run);
            }
        }
    }
}

