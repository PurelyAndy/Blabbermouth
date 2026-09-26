using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Blabbermouth.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Blabbermouth.Data;

public partial class Shocker : ObservableObject
{
    private static readonly HttpClient Client = new();

    public bool IsSerial { get; set; }
    [ObservableProperty] public partial bool IsEnabled { get; set; } = true;
    public string? ShareCode { get; set; }
    public string? ApiKey { get; set; }
    public string? ShockerID { get; set; }

    public Shocker()
    {
    }

    public Shocker(string shareCode, string apiKey)
    {
        ShareCode = shareCode;
        ApiKey = apiKey;
    }

    public Shocker(string shockerID)
    {
        ShockerID = shockerID;
        IsSerial = true;
    }

    public Shocker Clone()
    {
        return IsSerial
            ? new(ShockerID ?? string.Empty) { IsEnabled = IsEnabled }
            : new(ShareCode ?? string.Empty, ApiKey ?? string.Empty)
            {
                IsEnabled = IsEnabled,
            };
    }

    public async Task<(bool success, string? message)> Operate(int intensity, int ms, ShockerAction op)
    {
        if (!IsEnabled) return (true, null);

        if (IsSerial)
        {
            if (!int.TryParse(ShockerID, out int shockerID) || shockerID < 0)
            {
                return (false, "Invalid Shocker ID");
            }

            SerialPayload payload = new()
            {
                cmd = "operate",
                value = new SerialOperation
                {
                    id = shockerID,
                    op = op.ToString().ToLowerInvariant(),
                    duration = ms,
                    intensity = intensity,
                },
            };
            string json = JsonSerializer.Serialize(payload, JsonContext.Default.SerialPayload);

            if (PiShock.SerialPort is not { IsOpen: true })
            {
                return (false, "Serial port is not open. Please test your connection and try again.");
            }
            try
            {
                PiShock.SerialPort.WriteLine(json);
            }
            catch (Exception e)
            {
                return (false, $"Failed to send command over serial port:\n{e}");
            }

            return (true, json);
        }
        else
        {
            if (string.IsNullOrEmpty(ShareCode) || string.IsNullOrEmpty(ApiKey))
                return (false, "PiShock not configured");

            ApiPayload body = new()
            {
                AgentName = "Blabbermouth",
                Duration = ms,
                Intensity = intensity,
                Operation = (int)op,
            };

            string json = JsonSerializer.Serialize(body, JsonContext.Default.ApiPayload);
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json")
            {
                Headers =
                {
                    { "x-pishock-api-key", ApiKey },
                }
            };
            using HttpResponseMessage response = await Client.PostAsync($"https://api.pishock.com/Shockers/OperateByShare/{ShareCode}", content);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            string result = await response.Content.ReadAsStringAsync();

            return (false, response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "The API key you provided is not valid.",
                HttpStatusCode.Forbidden => $"Forbidden.\n{result}\nSend @PurelyAndy a screenshot of this message in the Blabbermouth thread in the PiShock Discord server.",
                HttpStatusCode.NotFound => "The share code you provided is not valid.",
                HttpStatusCode.MethodNotAllowed => $"The share code you provided does not have permission to perform a {op.ToString().ToLowerInvariant()} operation.",
                HttpStatusCode.NotAcceptable => "You must update your PiShock to V3 firmware.",
                HttpStatusCode.Gone => "The share code you provided is locked.",
                HttpStatusCode.PreconditionFailed => "The intensity is above the maximum allowed for the share code you provided.",
                HttpStatusCode.RequestedRangeNotSatisfiable => "The duration is above the maximum allowed for the share code you provided.",
                HttpStatusCode.ServiceUnavailable => "The share code you provided or the shocker it is tied to is currently paused.",
                _ => $"Unexpected response from PiShock API: {response.StatusCode} {response.ReasonPhrase}\n{result}\nSend @PurelyAndy a screenshot of this message in the Blabbermouth thread in the PiShock Discord server.",
            });
        }
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
    public required string AgentName { get; set; }
    public required int Duration { get; set; }
    public required int Intensity { get; set; }
    public required int Operation { get; set; }
}