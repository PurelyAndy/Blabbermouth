using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Data.Converters;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Blabbermouth.Data;

public class OperationSequence : ObservableCollection<Operation>
{
    public string AsString => ToString();
    
    public async Task Perform()
    {
        for (int i = 0; i < Count; i++)
        {
            await this[i].Perform(i == this.Count - 1);
        }
    }
    
    public OperationSequence Clone()
    {
        OperationSequence copy = [];
        foreach (Operation op in this)
        {
            copy.Add(op.Clone());
        }
        return copy;
    }

    /// <summary>
    /// Constructs a human-readable string describing the sequence of operations with proper punctuation and
    /// capitalization. The first operation is capitalized, and operations are separated by commas. Each group of
    /// operations that do not wait for completion get their own sentence, and sentences are separated by periods.
    /// </summary>
    /// <returns>
    /// A human-readable string describing the sequence of operations. E.g.<br/>
    /// [<br/>
    ///     Operation(Shock, 500, 15),<br/>
    ///     Operation(Vibration, 1000, 20) { WaitForCompletion = true },<br/>
    ///     Operation(Sound, "alert.wav"),<br/>
    ///     Operation(Shock, 1000, 30),<br/>
    ///     Operation(Wait, 2000) { WaitForCompletion = true },<br/>
    /// ]<br/>
    /// Results in:<br/>
    /// "Shock for 500ms at strength 15 and vibrate for 1000ms at strength 20. Play sound "alert.wav", shock for 1000ms at strength 30, and wait for 2000ms."
    /// </returns>
    public override string ToString()
    {
        if (Count == 0) return "Do nothing.";
        StringBuilder sb = new();
        int inSentence = 0;
        for (int i = 0; i < Count; i++)
        {
            Operation operation = this[i];
            Operation? next = i < Count - 1 ? this[i + 1] : null;
            Operation? nextNext = i < Count - 2 ? this[i + 2] : null;
            
            string opString = operation.ToString();
            if (i == 0)
                sb.Append(char.ToUpper(opString[0])).Append(opString[1..]);
            else
                sb.Append(opString);
            
            if (next is null)
            {
                sb.Append('.');
                break;
            }
            
            if (operation.WaitForCompletion || operation.Kind == OperationKind.Wait)
            {
                sb.Append(". Then, ");
                inSentence = 0;
            }
            else if (nextNext is null)
            {
                sb.Append(inSentence == 0 ? " and " : ", and ");
                inSentence++;
            }
            else
            {
                if (next.WaitForCompletion || next.Kind == OperationKind.Wait)
                {
                    sb.Append(inSentence == 0 ? " and " : ", and ");
                }
                else
                {
                    sb.Append(", ");
                }
                inSentence++;
            }
        }
        return sb.ToString();
    }
    
    protected override void InsertItem(int index, Operation item)
    {
        item.PropertyChanged += OnItemPropertyChanged;
        base.InsertItem(index, item);
        OnPropertyChanged(new(nameof(AsString)));
    }

    protected override void RemoveItem(int index)
    {
        Operation item = this[index];
        item.PropertyChanged -= OnItemPropertyChanged;
        base.RemoveItem(index);
        OnPropertyChanged(new(nameof(AsString)));
    }

    protected override void MoveItem(int oldIndex, int newIndex)
    {
        base.MoveItem(oldIndex, newIndex);
        OnPropertyChanged(new(nameof(AsString)));
    }

    protected override void SetItem(int index, Operation item)
    {
        this[index].PropertyChanged -= OnItemPropertyChanged;
        item.PropertyChanged += OnItemPropertyChanged;

        base.SetItem(index, item);
        OnPropertyChanged(new(nameof(AsString)));
    }

    protected override void ClearItems()
    {
        foreach (Operation item in this)
            item.PropertyChanged -= OnItemPropertyChanged;

        base.ClearItems();
        OnPropertyChanged(new(nameof(AsString)));
    }
    
    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(new(nameof(AsString)));
    }
    
    public bool EquivalentTo(OperationSequence? other)
    {
        if (other is null || Count != other.Count) return false;
        for (int i = 0; i < Count; i++)
        {
            if (!this[i].EquivalentTo(other[i])) return false;
        }
        return true;
    }
    
    public int GetHashCodeForEquivalence()
    {
        int hash = 17;
        foreach (Operation op in this)
        {
            hash = hash * 31 + op.GetHashCodeForEquivalence();
        }
        return hash;
    }
}

public class PlayPauseOperationToEmojiConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        bool waitForCompletion = values.Any(v => v is true);
        bool isWaitOp = values.Any(v => v is OperationKind.Wait);
        return waitForCompletion || isWaitOp ? "⏸️" : "▶️";
    }
}

public class WaitForCompletionTooltipConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        bool waitForCompletion = values.Any(v => v is true);
        bool isWaitOp = values.Any(v => v is OperationKind.Wait);
        return waitForCompletion || isWaitOp
            ? "This operation will wait for completion before starting the next one."
            : "This operation will not wait for completion before starting the next one.";
    }
}

public class DoesWaitConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        bool waitForCompletion = values.Any(v => v is true);
        bool isWaitOp = values.Any(v => v is OperationKind.Wait);
        return waitForCompletion || isWaitOp;
    }
}