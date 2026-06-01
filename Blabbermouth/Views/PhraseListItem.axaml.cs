using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Blabbermouth.Data;

namespace Blabbermouth.Views;

public partial class PhraseListItem : UserControl
{
    public event EventHandler<Data.PhraseEntry>? MoveUp;
    public event EventHandler<Data.PhraseEntry>? MoveDown;
    public event EventHandler<Data.PhraseEntry>? Remove;
    public event EventHandler<RoutedEventArgs>? EnterPressed;
    
    public PhraseListItem()
    {
        InitializeComponent();
        
        UpButton.Click += (_, _) => MoveUp?.Invoke(this, (Data.PhraseEntry)DataContext!);
        DownButton.Click += (_, _) => MoveDown?.Invoke(this, (Data.PhraseEntry)DataContext!);
        RemoveButton.Click += (_, _) => Remove?.Invoke(this, (Data.PhraseEntry)DataContext!);
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
        if (DataContext is not Data.PhraseEntry entry) return;
        entry.Activation = (Activation)(((int)entry.Activation % 3) + 1);
    }

    private void EffectButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not Data.PhraseEntry entry) return;
        entry.Effect = (Effect)(((int)entry.Effect + 1) % 3);
    }

    private void LengthChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (sender is not NumericUpDown nud) return;
        if (nud.Value == null)
        {
            nud.Value = 0.3m;
            return;
        }
        nud.Value = Math.Round(nud.Value.Value, 1);
    }

    private void SettingsButtonClicked(object? sender, RoutedEventArgs e)
    {
        SettingsPanel.IsVisible ^= true;
    }
}
