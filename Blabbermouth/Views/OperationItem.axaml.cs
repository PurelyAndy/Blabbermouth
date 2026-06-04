using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Blabbermouth.Data;

namespace Blabbermouth.Views;

public partial class OperationItem : UserControl
{
    public event EventHandler<Operation>? MoveUp;
    public event EventHandler<Operation>? MoveDown;
    public event EventHandler<Operation>? Remove;
    public OperationItem()
    {
        InitializeComponent();
        
        UpButton.Click += (_, _) => MoveUp?.Invoke(this, (Operation)DataContext!);
        DownButton.Click += (_, _) => MoveDown?.Invoke(this, (Operation)DataContext!);
        RemoveButton.Click += (_, _) => Remove?.Invoke(this, (Operation)DataContext!);
        
        DataContextChanged += (_, _) => UpdateVisibility();
    }

    private void KindButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not Operation op) return;
        op.Kind = (OperationKind)(((int)op.Kind + 1) % Enum.GetValues<OperationKind>().Length);
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (DataContext is not Operation op) return;
        LengthGroup.IsVisible = op.Kind is OperationKind.Shock or OperationKind.Vibration or OperationKind.Wait;
        StrengthGroup.IsVisible = op.Kind is OperationKind.Shock or OperationKind.Vibration;
        PathTextBox.IsVisible = op.Kind is OperationKind.Sound or OperationKind.Application;
    }

    private void WaitForCompletionButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not Operation op) return;
        op.WaitForCompletion = !op.WaitForCompletion;
    }
}