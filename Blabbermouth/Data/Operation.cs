using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading.Tasks;
using Blabbermouth.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Blabbermouth.Data;

public partial class Operation : ObservableObject
{
    [ObservableProperty] public partial OperationKind Kind { get; set; }
    [ObservableProperty] public partial double Length { get; set; }
    [ObservableProperty] public partial int Strength { get; set; }
    [ObservableProperty] public partial string FilePath { get; set; } = "";
    [ObservableProperty] public partial bool WaitForCompletion { get; set; }

    public Operation(OperationKind kind, double length, int strength)
    {
        if (kind is not OperationKind.Shock and not OperationKind.Vibration)
            throw new ArgumentException($"Must be {OperationKind.Shock} or {OperationKind.Vibration}.", nameof(kind));
        Kind = kind;
        Length = length;
        Strength = strength;
    }
    
    public Operation(OperationKind kind, string filePath)
    {
        if (kind is not OperationKind.Sound and not OperationKind.Application)
            throw new ArgumentException($"Must be {OperationKind.Sound} or {OperationKind.Application}.", nameof(kind));
        Kind = kind;
        FilePath = filePath;
    }
    
    public Operation(OperationKind kind, double length)
    {
        if (kind != OperationKind.Wait)
            throw new ArgumentException($"Must be {OperationKind.Wait}.", nameof(kind));
        Kind = kind;
        Length = length;
        WaitForCompletion = true;
    }
    
    public bool EquivalentTo(Operation? other)
    {
        if (other is null || Kind != other.Kind) return false;
        return Kind switch
        {
            OperationKind.Shock or OperationKind.Vibration => Length == other.Length && Strength == other.Strength,
            OperationKind.Sound or OperationKind.Application => FilePath == other.FilePath,
            OperationKind.Wait => Length == other.Length,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    public async Task Perform()
    {
        switch (Kind)
        {
            case OperationKind.Shock:
                await SttManager.Operate(Strength, Length, true);
                if (WaitForCompletion)
                {
                    await Task.Delay((int)(Length * 1000));
                }
                break;
            case OperationKind.Vibration:
                await SttManager.Operate(Strength, Length, false);
                if (WaitForCompletion)
                {
                    await Task.Delay((int)(Length * 1000));
                }
                break;
            case OperationKind.Sound:
                if (WaitForCompletion)
                    await PlaybackManager.PlayAudio(FilePath);
                else
                    _ = PlaybackManager.PlayAudio(FilePath);
                break;
            case OperationKind.Application:
                ProcessStartInfo psi = new(FilePath) { UseShellExecute = true };
                Process? process = Process.Start(psi);
                if (WaitForCompletion && process is not null)
                {
                    await process.WaitForExitAsync();
                }
                break;
            case OperationKind.Wait:
                await Task.Delay((int)(Length * 1000));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(Kind));
        }
    }
    
    public Operation Clone()
    {
        return Kind switch
        {
            OperationKind.Shock or OperationKind.Vibration => new(Kind, Length, Strength) 
                { WaitForCompletion = WaitForCompletion },
            OperationKind.Sound or OperationKind.Application => new(Kind, FilePath) 
                { WaitForCompletion = WaitForCompletion },
            OperationKind.Wait => new(Kind, Length),
            _ => throw new ArgumentOutOfRangeException(nameof(Kind)),
        };
    }

    public override string ToString()
    {
        return Kind switch
        {
            OperationKind.Shock => "shock for " + Length + " second" + (Length == 1 ? "" : "s") + " at strength " + Strength,
            OperationKind.Vibration => "vibrate for " + Length + " second" + (Length == 1 ? "" : "s") + " at strength " + Strength,
            OperationKind.Sound => "play sound '" + FilePath + "'",
            OperationKind.Application => "launch application '" + FilePath + "'",
            OperationKind.Wait => "wait for " + Length + "ms",
            _ => "???",
        };
    }
}

