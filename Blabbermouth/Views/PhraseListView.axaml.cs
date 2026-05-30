using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Blabbermouth.Views;

public partial class PhraseListView : UserControl
{
    private readonly ObservableCollection<PhraseEntry> _phrases = [];

    public List<PhraseEntry> Phrases => _phrases
        .Where(x => !string.IsNullOrWhiteSpace(x.Phrase))
        .ToList();
    public List<string> PhraseTexts => _phrases
        .Select(x => x.Phrase)
        .Where(p => !string.IsNullOrWhiteSpace(p))
        .ToList();
    
    public PhraseListView()
    {
        InitializeComponent();
        PhrasesBox.ItemsSource = _phrases;
    }

    private void AddClicked(object? sender, RoutedEventArgs e)
    {
        _phrases.Add(new());
        int newIndex = _phrases.Count - 1;

        Dispatcher.UIThread.Post(() =>
        {
            if (newIndex < 0) return;
            if (PhrasesBox.ContainerFromIndex(newIndex) is not ListBoxItem container) return;
            TextBox? textBox = container.GetVisualDescendants()
                .OfType<TextBox>()
                .FirstOrDefault(tb => tb.Name == "PhraseTextBox");
            textBox?.Focus();
        }, DispatcherPriority.Background);
    }

    private void RemovePhraseClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.DataContext is not PhraseEntry entry) return;
        _phrases.Remove(entry);
    }

    private void UpClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.DataContext is not PhraseEntry entry) return;
        int index = _phrases.IndexOf(entry);
        if (index > 0)
        {
            _phrases.Move(index, index - 1);
        }
    }

    private void DownClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.DataContext is not PhraseEntry entry) return;
        int index = _phrases.IndexOf(entry);
        if (index >= 0 && index < _phrases.Count - 1)
        {
            _phrases.Move(index, index + 1);
        }
    }

    private void PhraseKeyPressed(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AddClicked(sender, null!);
        }
    }

    private void PhraseLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb) return;
        if (tb.DataContext is not PhraseEntry entry) return;

        if (string.IsNullOrWhiteSpace(entry.Phrase))
        {
            _phrases.Remove(entry);
        }
    }

    private void EffectButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.DataContext is not PhraseEntry entry) return;

        entry.Effect = (Effect)(((int)entry.Effect + 1) % 3);
        _phrases[_phrases.IndexOf(entry)] = entry;
    }

    private void ActivationButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.DataContext is not PhraseEntry entry) return;

        entry.Activation = (Activation)(((int)entry.Activation % 3) + 1);
        _phrases[_phrases.IndexOf(entry)] = entry;
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

    public void ImportPhrases(string json)
    {
        List<PhraseEntry>? importedPhrases = JsonSerializer.Deserialize<List<PhraseEntry>>(json, PhraseListJsonContext.Default.ListPhraseEntry);
        if (importedPhrases == null) return;
        
        _phrases.Clear();
        foreach (PhraseEntry entry in importedPhrases)
        {
            _phrases.Add(entry);
        }
    }
}

