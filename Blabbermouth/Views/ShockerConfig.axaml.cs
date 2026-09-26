using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Media;
using Blabbermouth.Core;
using Blabbermouth.Data;
using Blabbermouth.Windows;

namespace Blabbermouth.Views;

public partial class ShockerConfig : UserControl
{
    public ShockerConfig()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => UpdateModePresentation();
        UpdateModePresentation();
    }

    private Shocker? CurrentShocker => DataContext as Shocker;

    private void UpdateModePresentation()
    {
        if (CurrentShocker is null) return;

        ApiGrid.IsVisible = !CurrentShocker.IsSerial;
        SerialGrid.IsVisible = CurrentShocker.IsSerial;
        ModeButton.Content = CurrentShocker.IsSerial ? "Use API credentials" : "Use serial port";
    }

    private void ToggleModeClicked(object? sender, RoutedEventArgs e)
    {
        if (CurrentShocker is null) return;

        CurrentShocker.IsSerial = !CurrentShocker.IsSerial;
        UpdateModePresentation();
    }

    private async void TestClicked(object? sender, RoutedEventArgs e)
    {
        if (CurrentShocker is null) return;

        string? result = await TestShockerAsync(CurrentShocker);
        if (result is null)
        {
            await GetParentWindow().ShowErrorAsync("The selected shocker test command was sent successfully.", "Test succeeded!");
        }
        else
        {
            await GetParentWindow().ShowErrorAsync(result, "Test failed");
        }
    }

    private static async Task<string?> TestShockerAsync(Shocker shocker)
    {
        if (shocker.IsSerial)
        {
            string? port = Settings.Get<string>("serialPort");
            if (string.IsNullOrWhiteSpace(port))
            {
                return "No serial port has been selected. Choose a serial port in the shocker configuration window first.";
            }

            if (PiShock.SerialPort is null || !PiShock.SerialPort.IsOpen || !string.Equals(PiShock.SerialPort.PortName, port, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    PiShock.ResetSerialPort(port);
                }
                catch (UnauthorizedAccessException)
                {
                    return "Access to the selected serial port is denied. Make sure no other applications are using the port, and try running Blabbermouth as an administrator.";
                }
            }
        }

        (bool success, string? message) result = await shocker.Operate(50, 1000, ShockerAction.Vibrate);
        return result.success ? null : result.message ?? "The test command failed.";
    }

    private ShockerConfigWindow GetParentWindow()
    {
        if (VisualRoot is ShockerConfigWindow window)
        {
            return window;
        }

        throw new InvalidOperationException("ShockerConfig must be hosted inside a ShockerConfigWindow.");
    }
}


public class EnabledToBackgroundConverter : IValueConverter
{
    private static readonly SolidColorBrush DisabledBrush = new(Color.FromRgb(0x30, 0x30, 0x30));
    private static readonly SolidColorBrush EnabledBrush = new(Color.FromRgb(0x20, 0x20, 0x20));
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool enabled)
        {
            return enabled ? EnabledBrush : DisabledBrush;
        }
        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
