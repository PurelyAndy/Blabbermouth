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
            textBox?.SelectAll();
        }, DispatcherPriority.Background);
    }

    private void PhraseRemove(object? sender, PhraseEntry e)
    {
        _phrases.Remove(e);
    }

    private void MovePhraseUp(object? sender, PhraseEntry e)
    {
        int index = _phrases.IndexOf(e);
        if (index > 0)
        {
            _phrases.Move(index, index - 1);
        }
    }

    private void MovePhraseDown(object? sender, PhraseEntry e)
    {
        int index = _phrases.IndexOf(e);
        if (index >= 0 && index < _phrases.Count - 1)
        {
            _phrases.Move(index, index + 1);
        }
    }

    private void PhraseEnterPressed(object? sender, RoutedEventArgs e)
    {
        AddClicked(sender, null!);
    }

    private void PhraseLostFocus(object? sender, RoutedEventArgs e)
    {
        var entry = ((sender as PhraseEntryView)!.DataContext as PhraseEntry)!;
        if (string.IsNullOrWhiteSpace(entry.Phrase))
        {
            _phrases.Remove(entry);
        }
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

