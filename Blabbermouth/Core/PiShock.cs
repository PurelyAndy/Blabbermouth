using System;
using System.IO.Ports;
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
    private static readonly HttpClient Client = new();

    public static string? Username = null;
    public static string? ShareCode = null;
    public static string? ApiKey = null;
    public static SerialPort? SerialPort;
    public static int ShockerID;

    public static async Task<string> Operate(int intensity, int duration, ShockerAction op)
    {
        if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(ShareCode) || string.IsNullOrEmpty(ApiKey))
            return "PiShock not configured";
        
        var body = new ApiPayload
        {
            code = ShareCode,
            duration = duration,
            intensity = intensity,
            op = (int)op,
            apikey = ApiKey,
            username = Username,
            name = "Blabbermouth",
        };
        
        string json = JsonSerializer.Serialize(body, PiShockJsonContext.Default.ApiPayload);
        HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await Client.PostAsync("https://ps.pishock.com/PiShock/Operate", content);
        return await response.Content.ReadAsStringAsync();
    }

    public static async Task<string> SerialOperate(int intensity, int ms, ShockerAction op)
    {
        var payload = new SerialPayload
        {
            cmd = "operate",
            value = new SerialOperation
            {
                id = ShockerID,
                op = op.ToString().ToLowerInvariant(),
                duration = ms,
                intensity = intensity,
            },
        };
        string json = JsonSerializer.Serialize(payload, PiShockJsonContext.Default.SerialPayload);
        
        if (SerialPort is not { IsOpen: true })
        {
            await DialogHost.Show(new DialogBox(MainWindow.I.Dialog,
                "Serial port is not open. Please test your connection and try again.",
                "Error", "OK"), MainWindow.I.Dialog);
            return json;
        }
        try
        {
            SerialPort.WriteLine(json);
        }
        catch (Exception e)
        {
            await DialogHost.Show(new DialogBox(MainWindow.I.Dialog,
                $"Failed to send command over serial port:\n{e}",
                "Serial communication error", "OK"), MainWindow.I.Dialog);
        }

        return json;
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
}

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Global
public class SerialPayload
{
    public required string cmd { get; set; }
    public required object value { get; set; }
}
public class SerialOperation
{
    public required int id { get; set; }
    public required string op { get; set; }
    public required int duration { get; set; }
    public required int intensity { get; set; }
}
public class ApiPayload
{
    public required string code { get; set; }
    public required int duration { get; set; }
    public required int intensity { get; set; }
    public required int op { get; set; }
    public required string apikey { get; set; }
    public required string username { get; set; }
    public required string name { get; set; }
}

[JsonSerializable(typeof(SerialPayload))]
[JsonSerializable(typeof(SerialOperation))]
[JsonSerializable(typeof(ApiPayload))]
public partial class PiShockJsonContext : JsonSerializerContext;