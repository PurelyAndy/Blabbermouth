using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Blabbermouth.Data;

namespace Blabbermouth.Views;

public partial class SpeechRecognitionMonitor : UserControl
{
    private readonly List<(Activation Activation, bool Partial, List<TextSegment> Segments)> _monitorTextSegments = [];

    public SpeechRecognitionMonitor()
    {
        InitializeComponent();
    }

    public void AddSegments(Activation activation, bool partial, List<TextSegment> segments)
    {
        int existingIndex = _monitorTextSegments.FindIndex(entry => entry.Activation == activation && entry.Partial);
        if (existingIndex != -1)
        {
            _monitorTextSegments[existingIndex] = (activation, partial, segments);
        }
        else
        {
            _monitorTextSegments.Add((activation, partial, segments));
        }
        ClampText();
        ApplyStyles();
        OutputScroller.ScrollToEnd();
    }

    private void ClampText()
    {
        const int maxLines = 50;

        _monitorTextSegments.RemoveRange(0, Math.Max(0, _monitorTextSegments.Count - maxLines));
    }

    private void ApplyStyles()
    {
        if (_monitorTextSegments.Count == 0)
            return;

        MonitorBlock.Text = null;
        MonitorBlock.Inlines?.Clear();

        foreach ((_, _, List<TextSegment> segments) in _monitorTextSegments)
        foreach (TextSegment segment in segments.Where(segment => !string.IsNullOrEmpty(segment.Text)))
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
                Run run = new(segment.Text)
                {
                    Foreground = segment.Foreground,
                    Background = segment.Background,
                };
                MonitorBlock.Inlines?.Add(run);
            }
        }
    }
}

