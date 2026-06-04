using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Blabbermouth.Data;
using DialogHostAvalonia;

namespace Blabbermouth.Windows;

public partial class SequenceEditorWindow : Window
{
    private readonly OperationSequence _originalOperations;
    public SequenceEditorWindow()
    {
        InitializeComponent();
        OperationSequence ops = [];
        DataContext = ops;
        OperationList.ItemsSource = ops;
        _originalOperations = ops;
    }
    
    public SequenceEditorWindow(OperationSequence operations) : this()
    {
        DataContext = operations;
        OperationList.ItemsSource = operations;
        _originalOperations = operations;
    }

    private void AddClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not OperationSequence ops) return;
        ops.Add(new(OperationKind.Vibration, 1, 20));
    }

    private void OperationRemove(object? sender, Operation e)
    {
        if (DataContext is not OperationSequence ops) return;
        ops.Remove(e);
    }

    private void MoveOperationUp(object? sender, Operation e)
    {
        if (DataContext is not OperationSequence ops) return;
        int index = ops.IndexOf(e);
        if (index > 0)
        {
            ops.Move(index, index - 1);
        }
    }
    
    private void MoveOperationDown(object? sender, Operation e)
    {
        if (DataContext is not OperationSequence ops) return;
        int index = ops.IndexOf(e);
        if (index < ops.Count - 1)
        {
            ops.Move(index, index + 1);
        }
    }

    private void DoneClicked(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private async void CancelClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not OperationSequence ops) return;
        if (!ops.EquivalentTo(_originalOperations))
        {
            object? result = await DialogHost.Show(new DialogBox(Dialog, "Are you sure you want to discard your changes?", "Sequence modified", "Yes", "No"), Dialog);
            if (result is "No")
                return;
        }
        Close(false);
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is not OperationSequence ops) return;
        if (!ops.EquivalentTo(_originalOperations))
        {
            object? result = await DialogHost.Show(new DialogBox(Dialog, "Are you sure you want to discard your changes?", "Sequence modified", "Yes", "No"), Dialog);
            if (result is "No")
                e.Cancel = true;
        }
    }

    private async void TestClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not OperationSequence ops) return;
        await ops.Perform();
    }
}