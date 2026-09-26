using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Blabbermouth.Core;
using Blabbermouth.Data;
using Blabbermouth.Views;
using DialogHostAvalonia;

namespace Blabbermouth.Windows;

public partial class ShockerConfigWindow : Window
{
    private ObservableCollection<Shocker> ShockerConfigs { get; } = [];
    private ObservableCollection<string> SerialPorts { get; } = [];

    public ShockerConfigWindow()
    {
        InitializeComponent();

        ShockersListControl.ItemsSource = ShockerConfigs;
        SerialPortsBox.ItemsSource = SerialPorts;

        LoadSerialPorts();
        LoadShockers();
    }

    public async Task ShowErrorAsync(string message, string title)
    {
        await DialogHost.Show(new DialogBox(Dialog, message, title, "OK"), Dialog);
    }

    private void LoadShockers()
    {
        ShockerConfigs.Clear();
        foreach (Shocker shocker in PiShock.Shockers)
        {
            ShockerConfigs.Add(shocker.Clone());
        }
    }

    private void LoadSerialPorts()
    {
        SerialPorts.Clear();

        string[] ports = SerialPort.GetPortNames();
        if (ports.Length == 0)
        {
            SerialPorts.Add("No serial ports found.");
            SerialPorts.Add("Plug your hub in and restart Blabbermouth.");
        }
        else
        {
            foreach (string port in ports)
            {
                SerialPorts.Add(port);
            }
        }

        string? savedPort = Settings.Get<string>("serialPort");
        if (!string.IsNullOrWhiteSpace(savedPort) && SerialPorts.Contains(savedPort))
        {
            SerialPortsBox.SelectedItem = savedPort;
        }
        else if (SerialPorts.Count > 0)
        {
            SerialPortsBox.SelectedIndex = 0;
        }
    }

    private void SerialPortsBoxSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        string? selectedPort = SerialPortsBox.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(selectedPort)
            || selectedPort.StartsWith("No serial ports")
            || selectedPort.StartsWith("Plug your hub"))
        {
            Settings.Set("serialPort", "");
            return;
        }

        Settings.Set("serialPort", selectedPort);
    }

    private void AddShockerClicked(object? sender, RoutedEventArgs e)
    {
        ShockerConfigs.Add(new(string.Empty, string.Empty));
    }

    private void RemoveShockerClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: Shocker shocker }) return;
        ShockerConfigs.Remove(shocker);
    }

    private async void SaveClicked(object? sender, RoutedEventArgs e)
    {
        PiShock.Shockers.Clear();
        foreach (Shocker shocker in ShockerConfigs)
        {
            string? result = PiShock.ValidateShocker(shocker);
            if (result is not null)
            {
                await ShowErrorAsync(result, "Invalid shocker configuration");
                return;
            }
        }

        foreach (Shocker shocker in ShockerConfigs)
        {
            PiShock.Shockers.Add(shocker.Clone());
        }

        Settings.Set("shockers", JsonSerializer.Serialize(PiShock.Shockers, JsonContext.Default.ListShocker));
        Close(true);
    }

    private void CancelClicked(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}


