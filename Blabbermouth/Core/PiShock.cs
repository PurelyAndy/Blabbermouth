using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Blabbermouth.Data;
using Blabbermouth.Windows;
using DialogHostAvalonia;

namespace Blabbermouth.Core;

public static class PiShock
{
    public static readonly List<Shocker> Shockers = [];
    public static SerialPort? SerialPort;

    public static async Task<(bool success, string? message)[]> Operate(int intensity, int ms, ShockerAction op)
    {
        var results = new Task<(bool, string?)>[Shockers.Count];
        for (int i = 0; i < Shockers.Count; i++)
        {
            results[i] = Shockers[i].Operate(intensity, ms, op);
        }
        return await Task.WhenAll(results);
    }

    public static void ResetSerialPort(string port)
    {
        SerialPort?.Dispose();

        SerialPort = new(port, 115200)
        {
            Parity = Parity.None,
            DataBits = 8,
            StopBits = StopBits.One,
        };
        if (!OperatingSystem.IsWindows())
        {
            SerialPort.ReadTimeout = 3000;
        }
        SerialPort.Open();
    }

    public static void ClearSerialPort()
    {
        SerialPort?.Close();
        SerialPort = null;
    }

    public static string? ValidateShocker(Shocker shocker)
    {
        if (!shocker.IsSerial)
        {
            if (string.IsNullOrWhiteSpace(shocker.ShareCode) || string.IsNullOrWhiteSpace(shocker.ApiKey))
            {
                return "One of the enabled API shockers is missing a username, share code, or API key. Please fix it in the shocker configuration menu.";
            }

            return null;
        }

        if (string.IsNullOrWhiteSpace(shocker.ShockerID) || !int.TryParse(shocker.ShockerID, out int shockerId) || shockerId < 0)
        {
            return "One of the enabled serial shockers has an invalid shocker ID. Please fix it in the shocker configuration menu.";
        }

        if (!shocker.IsEnabled)
        {
            return null;
        }

        string? port = Settings.Get<string>("serialPort");
        if (string.IsNullOrWhiteSpace(port) || port.StartsWith("No serial ports"))
        {
            return "A serial shocker is enabled, but no valid serial port is selected. Open the shocker configuration menu and select a port.";
        }

        try
        {
            if (SerialPort is null || !SerialPort.IsOpen || !string.Equals(SerialPort.PortName, port, StringComparison.OrdinalIgnoreCase))
            {
                ResetSerialPort(port);
            }
        }
        catch (UnauthorizedAccessException)
        {
            return "Access to the selected serial port is denied. Make sure no other applications are using the port, and try running Blabbermouth as an administrator.";
        }
        catch (Exception ex)
        {
            return $"Failed to open the selected serial port:\n{ex.Message}";
        }

        return null;
    }

    public static string? ValidateShockers()
    {
        List<Shocker> enabledShockers = Shockers.Where(shocker => shocker.IsEnabled).ToList();
        if (enabledShockers.Count == 0)
        {
            return "No shockers are enabled. Open the shocker configuration menu and add or enable at least one shocker.";
        }

        foreach (Shocker shocker in enabledShockers)
        {
            return ValidateShocker(shocker);
        }

        return null;
    }
}
