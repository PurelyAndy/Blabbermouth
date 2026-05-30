using System;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DialogHostAvalonia;

namespace Blabbermouth.Views;

public partial class ShockerConfigView : UserControl
{
    private bool HasSerialPorts { get; set; }
    public bool UsingSerial { get; private set; }

    public ShockerConfigView()
    {
        InitializeComponent();

        UsernameTextBox.Text = Settings.Get<string>("username");
        ShareCodeTextBox.Text = Settings.Get<string>("shareCode");
        ApiKeyTextBox.Text = Settings.Get<string>("apiKey");
        ShockerIdBox.Text = Settings.Get<string>("shockerId");

        if (!OperatingSystem.IsWindows())
        {
            // on non-windows OSes, the functionality is bugged? or at least it is on my linux machine.
            // so, it's not actually lower-latency in that case. still easier to set up, though.
            UseSerialButton.Content = "Use Serial Port";
        }

        PopulateSerialPorts();
    }

    private void PopulateSerialPorts()
    {
        string[] ports = SerialPort.GetPortNames();
        if (ports.Length == 0)
        {
            ports = ["No serial ports found.", "Plug your hub in and restart the application."];
            HasSerialPorts = false;
        }
        else
        {
            HasSerialPorts = true;
        }

        SerialPortsBox.ItemsSource = ports;
        SerialPortsBox.SelectionMode = SelectionMode.Single;
        SerialPortsBox.SelectedIndex = 0;
    }

    public string? SelectedPort => SerialPortsBox.SelectedItem as string;

    public int ShockerID => string.IsNullOrEmpty(ShockerIdBox.Text) ? 0 : int.TryParse((string?)ShockerIdBox.Text, out int i) ? i : 0;

    public string Username => UsernameTextBox.Text ?? string.Empty;
    public string ShareCode => ShareCodeTextBox.Text ?? string.Empty;
    public string ApiKey => ApiKeyTextBox.Text ?? string.Empty;

    public void SetUsingSerial(bool usingSerial)
    {
        UsingSerial = usingSerial;
        SerialGrid.IsVisible = usingSerial;
        ApiCredsGrid.IsVisible = !usingSerial;
    }

    private void UseSerialClicked(object? sender, RoutedEventArgs e)
    {
        SetUsingSerial(true);
    }

    private void UseApiClicked(object? sender, RoutedEventArgs e)
    {
        SetUsingSerial(false);
    }

    private async void TestCredentialsClicked(object? sender, RoutedEventArgs e)
    {
        string? result = await TestCredentialsAsync();
        if (result != null)
        {
            await DialogHost.Show(new DialogBox(MainWindow.I.Dialog, result, "Test failed", "OK"), MainWindow.I.Dialog);
            return;
        }

        await DialogHost.Show(new DialogBox(MainWindow.I.Dialog,
            "The information you entered is valid and Blabbermouth should work correctly.\n" +
            "If Blabbermouth isn't working, who knows why. Ask @PurelyAndy in the Blabbermouth thread in the PiShock Discord server.",
            "Test succeeded!", "OK"), MainWindow.I.Dialog);
    }

    private async void TestPortClicked(object? sender, RoutedEventArgs e)
    {
        await TestPortAsync();
    }

    public async Task<string?> TestCredentialsAsync()
    {
        string username = Username;
        string shareCode = ShareCode;
        string apiKey = ApiKey;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(shareCode) ||
            string.IsNullOrWhiteSpace(apiKey))
        {
            return "Please fill in all the fields: username, share code, and API key.";
        }

        PiShock.Username = username;
        PiShock.ShareCode = shareCode;
        PiShock.ApiKey = apiKey;

        string result = await PiShock.Operate(50, 1000, false);
        return result switch
        {
            "\"Operation Attempted.\"" => null,
            "\"Not Authorized.\"" => "The API key you provided is not valid, or it is not tied to your username.",
            "\"This code doesn't exist.\"" => "The share code you provided is not valid.",
            "\"This share code has already been used by somebody else.\"" => "The share code you provided has been claimed by another user. Ensure you have entered the correct username.",
            _ => $"Unexpected response from PiShock API:\n{result}\nAsk @PurelyAndy in the Blabbermouth thread in the PiShock Discord server with a screenshot of this message.",
        };
    }

    public async Task TestPortAsync()
    {
        if (!HasSerialPorts) return;
        string? selectedPort = SelectedPort;
        if (string.IsNullOrWhiteSpace(selectedPort) || selectedPort.StartsWith("No serial ports"))
        {
            await DialogHost.Show(new DialogBox(MainWindow.I.Dialog,
                "No serial port selected. Please select a valid serial port from the dropdown menu.",
                "Error", "OK"), MainWindow.I.Dialog);
            return;
        }

        if (ShockerID == 0)
        {
            await DialogHost.Show(new DialogBox(MainWindow.I.Dialog,
                "You must enter a shocker ID.",
                "Error", "OK"), MainWindow.I.Dialog);
            return;
        }
        PiShock.ShockerID = ShockerID;

        try
        {
            PiShock.ResetSerialPort(selectedPort);
        }
        catch (UnauthorizedAccessException)
        {
            await DialogHost.Show(new DialogBox(MainWindow.I.Dialog,
                "Access to the selected serial port is denied. This can happen if another application is using the port, or if you don't have permission to access it.\n" +
                "Make sure no other applications are using the port, and try running Blabbermouth as an administrator.",
                "Error, port not selected", "OK"), MainWindow.I.Dialog);
            goto cleanup;
        }

        await PiShock.SerialOperate(50, 1000, false);
        await Task.Delay(1500);
        string response = string.Empty;
        try
        {
            while (true)
            {
                try
                {
                    response += PiShock.SerialPort!.ReadTo("\r\n") + "\n";
                }
                catch (InvalidOperationException)
                {
                    await DialogHost.Show(new DialogBox(MainWindow.I.Dialog,
                        "The serial port is not open.",
                        "Error, port not selected", "OK"), MainWindow.I.Dialog);
                    goto cleanup;
                }
            }
        }
        catch (TimeoutException)
        {
            if (string.IsNullOrEmpty(response))
            {
                await DialogHost.Show(new DialogBox(MainWindow.I.Dialog,
                    "No response received from the serial device. This is probably the wrong one.",
                    "Error, port not selected", "OK"), MainWindow.I.Dialog);
                goto cleanup;
            }
        }
        catch (IOException)
        {
            // Ignored - happens when there's nothing left to read? I think?
        }

        if (response.StartsWith("Received JSON:"))
        {
            await DialogHost.Show(new DialogBox(MainWindow.I.Dialog,
                "Test command sent successfully! If your shocker vibrated, everything is working correctly. " +
                "If it didn't, make sure the shocker ID is correct or try a different port.",
                "Port selected successfully", "OK"), MainWindow.I.Dialog);
            return;
        }

        object? result = await DialogHost.Show(new DialogBox(MainWindow.I.DialogNoClickAway,
            $"Unexpected response from the serial device:\n{response}\n" +
            "If your device did not vibrate, this is the wrong port. Otherwise, it's a-ok.",
            "Error? Port will be selected if it vibrated", "It vibrated", "It didn't vibrate"), MainWindow.I.DialogNoClickAway);
        if (result is "It vibrated")
        {
            return;
        }

        cleanup:
        PiShock.ClearSerialPort();
    }

    private void ShockerIdChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        if (string.IsNullOrEmpty(textBox.Text)) return;
        if (!int.TryParse(textBox.Text, out int id) || id < 0)
        {
            textBox.Text = "0";
        }
        Settings.Set("shockerId", textBox.Text);
    }
}


