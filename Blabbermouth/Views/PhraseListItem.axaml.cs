using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Blabbermouth.Data;
using Blabbermouth.Windows;

namespace Blabbermouth.Views;

public partial class PhraseListItem : UserControl
{
    public event EventHandler<PhraseEntry>? MoveUp;
    public event EventHandler<PhraseEntry>? MoveDown;
    public event EventHandler<PhraseEntry>? Remove;
    public event EventHandler<RoutedEventArgs>? EnterPressed;

    public PhraseListItem()
    {
        InitializeComponent();

        UpButton.Click += (_, _) => MoveUp?.Invoke(this, (PhraseEntry)DataContext!);
        DownButton.Click += (_, _) => MoveDown?.Invoke(this, (PhraseEntry)DataContext!);
        RemoveButton.Click += (_, _) => Remove?.Invoke(this, (PhraseEntry)DataContext!);
    }

    private void PhraseKeyPressed(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            EnterPressed?.Invoke(this, new());
            e.Handled = true;
        }
    }

    private void ActivationButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PhraseEntry entry) return;
        entry.Activation = (Activation)(((int)entry.Activation % 3) + 1);
    }

    private async void SequenceButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PhraseEntry entry) return;

        OperationSequence workingCopy = entry.Operations.Clone();
        SequenceEditorWindow window = new(workingCopy);

        if (TopLevel.GetTopLevel(this) is not Window parentWindow) return;

        if (await window.ShowDialog<bool>(parentWindow))
        {
            entry.Operations = workingCopy;
        }
    }

    private void SettingsButtonClicked(object? sender, RoutedEventArgs e)
    {
        SettingsPanel.IsVisible ^= true;
    }
}
